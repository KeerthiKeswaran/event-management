import { Component, EventEmitter, Input, Output, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CancelEventModalComponent } from '../cancel-event-modal/cancel-event-modal';
import { firstValueFrom } from 'rxjs';
import * as CryptoJS from 'crypto-js';
import { EventService } from '../../../services/event.service';
import { environment } from '../../../../environments/environment';


@Component({
  selector: 'app-event-details-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, CancelEventModalComponent],
  templateUrl: './event-details-modal.html',
  styleUrl: './event-details-modal.css'
})
export class EventDetailsModalComponent implements OnInit {
  public showCancelModal = false;
  @Input() event: any;
  @Output() close = new EventEmitter<void>();
  @Output() updated = new EventEmitter<void>();
  @Output() recreate = new EventEmitter<void>();

  public isPasswordVisible = false;
  
  public isEditingTitle = false;
  public editedTitle = '';
  public maxTitleUpdates = 2;
  
  public isEditingDescription = false;
  public editedDescription = '';
  public originalDescriptionText = '';
  public isFetchingDescription = false;

  public isSubmitting = false;
  public errorMessage = '';

  public openCancelModal(): void {
    this.showCancelModal = true;
  }

  public closeCancelModal(): void {
    this.showCancelModal = false;
  }

  public onEventCancelled(updatedEvent: any): void {
    this.showCancelModal = false;
    this.event.status = 'Cancelled';
    this.updated.emit();
  }

  constructor(
    private eventService: EventService,
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  async ngOnInit() {
    this.editedTitle = this.event.title;
    
    // Fetch actual description text from the URL
    if (this.event.description_Url) {
      this.isFetchingDescription = true;
      try {
        const baseUrl = this.event.description_Url.startsWith('http') 
          ? this.event.description_Url 
          : `${environment.blobStorageUrl}/${this.event.description_Url}`;
        // Append cache-buster so browser always fetches the latest version
        const url = `${baseUrl}?t=${Date.now()}`;
        const text = await firstValueFrom(this.http.get(url, { responseType: 'text' }));
        this.originalDescriptionText = text;
        this.editedDescription = text;
      } catch (err) {
        console.error('Failed to load description text', err);
        this.originalDescriptionText = 'Failed to load description.';
        this.editedDescription = this.originalDescriptionText;
      } finally {
        this.isFetchingDescription = false;
        this.cdr.detectChanges();
      }
    }
  }

  togglePasswordVisibility() {
    this.isPasswordVisible = !this.isPasswordVisible;
  }

  get decodedPassword(): string {
    if (!this.event?.virtual_Password_Hash) return '••••••••••••';
    try {
      const secretKey = 'EventManagementSuperSecretKey32!';
      const key = CryptoJS.enc.Utf8.parse(secretKey.padEnd(32, ' ').substring(0, 32));
      const iv = CryptoJS.enc.Hex.parse('00000000000000000000000000000000');

      const decrypted = CryptoJS.AES.decrypt(this.event.virtual_Password_Hash, key, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
      });

      const result = decrypted.toString(CryptoJS.enc.Utf8);
      return result || this.event.virtual_Password_Hash;
    } catch (err) {
      console.error('Decryption failed', err);
      return this.event.virtual_Password_Hash;
    }
  }

  public copiedState: 'url' | 'password' | '' = '';

  copyToClipboard(text: string | null, type: 'url' | 'password') {
    if (text) {
      navigator.clipboard.writeText(text);
      this.copiedState = type;
      setTimeout(() => {
        if (this.copiedState === type) {
          this.copiedState = '';
          this.cdr.detectChanges();
        }
      }, 2000);
    }
  }

  enableTitleEdit() {
    if ((this.event.title_Update_Count || 0) >= this.maxTitleUpdates) return;
    this.isEditingTitle = true;
  }

  enableDescriptionEdit() {
    this.isEditingDescription = true;
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = this.originalDescriptionText;
    this.editedDescription = tempDiv.innerText || tempDiv.textContent || '';
  }

  cancelEdit() {
    this.isEditingTitle = false;
    this.isEditingDescription = false;
    this.editedTitle = this.event.title;
    this.editedDescription = this.originalDescriptionText;
    this.errorMessage = '';
  }

  isHtml(text: string): boolean {
    return /<\/?[a-z][\s\S]*>/i.test(text || '');
  }

  async saveChanges() {
    if (this.isSubmitting) return;
    
    let updates: any = {};
    if (this.isEditingTitle && this.editedTitle !== this.event.title) {
      if ((this.event.title_Update_Count || 0) >= this.maxTitleUpdates) {
        this.errorMessage = 'You have reached the maximum number of title updates.';
        return;
      }
      updates.title = this.editedTitle;
    }
    
    if (this.isEditingDescription && this.editedDescription !== this.originalDescriptionText) {
      updates.descriptionText = this.editedDescription;
    }
    
    if (Object.keys(updates).length === 0) {
      this.cancelEdit();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    this.eventService.updateEventDetails(this.event.event_Id, updates).subscribe({
      next: () => {
        this.isSubmitting = false;
        // Update local state immediately so UI reflects the saved values
        if (this.isEditingTitle && updates.title) {
          this.event.title = updates.title;
          this.event.title_Update_Count = (this.event.title_Update_Count || 0) + 1;
        }
        if (this.isEditingDescription && updates.descriptionText !== undefined) {
          this.originalDescriptionText = updates.descriptionText;
          this.editedDescription = updates.descriptionText;
        }
        this.isEditingTitle = false;
        this.isEditingDescription = false;
        this.updated.emit();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.message || 'Failed to update event details.';
      }
    });
  }

  public triggerRecreate(e?: Event) {
    if (e) {
      e.stopPropagation();
    }
    this.recreate.emit();
  }

  closeModal() {
    this.close.emit();
  }
}
