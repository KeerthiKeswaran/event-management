import { Component, ElementRef, ViewChild, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { NavbarComponent } from '../../home/navbar/navbar';
import { FooterComponent } from '../../home/footer/footer';
import jsQR from 'jsqr';

@Component({
  selector: 'app-checkin',
  standalone: true,
  imports: [CommonModule, NavbarComponent, FooterComponent],
  templateUrl: './checkin.html',
  styleUrls: ['./checkin.css']
})
export class CheckinComponent {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('canvas') canvas!: ElementRef<HTMLCanvasElement>;

  public isDragging = signal(false);
  public isScanning = signal(false);
  public successMessage = signal<string | null>(null);
  public errorMessage = signal<string | null>(null);

  constructor(private http: HttpClient) {}

  public onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging.set(true);
  }

  public onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }

  public onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
    this.errorMessage.set(null);

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.handleFile(event.dataTransfer.files[0]);
    } else if (event.dataTransfer?.items) {
      let found = false;
      for (let i = 0; i < event.dataTransfer.items.length; i++) {
        const item = event.dataTransfer.items[i];
        if (item.type.indexOf('image/') === 0) {
          const file = item.getAsFile();
          if (file) {
            this.handleFile(file);
            found = true;
            break;
          }
        }
      }
      if (!found) {
        const html = event.dataTransfer.getData('text/html');
        if (html) {
          const div = document.createElement('div');
          div.innerHTML = html;
          const img = div.querySelector('img');
          if (img && img.src) {
             this.fetchAndProcessImageUrl(img.src);
             found = true;
          }
        }
      }
      if (!found) {
        this.errorMessage.set('No valid image found in the dropped content.');
      }
    }
  }

  private async fetchAndProcessImageUrl(url: string): Promise<void> {
     try {
        this.isScanning.set(true);
        const response = await fetch(url);
        const blob = await response.blob();
        if (blob.type.indexOf('image/') !== 0) {
           this.errorMessage.set('Dragged URL is not a valid image.');
           this.isScanning.set(false);
           return;
        }
        const file = new File([blob], 'dragged-image.jpg', { type: blob.type });
        this.handleFile(file);
     } catch (err) {
        this.errorMessage.set('Could not fetch the dragged image due to cross-origin restrictions or network error.');
        this.isScanning.set(false);
     }
  }

  public onFileSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  public triggerFileInput() {
    this.fileInput.nativeElement.click();
  }

  private handleFile(file: File) {
    if (!file.type.startsWith('image/')) {
      this.errorMessage.set('Please upload a valid image file.');
      return;
    }

    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.isScanning.set(true);

    const reader = new FileReader();
    reader.onload = (e) => {
      const img = new Image();
      img.onload = () => {
        const canvas = this.canvas.nativeElement;
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          this.isScanning.set(false);
          this.errorMessage.set('Failed to initialize canvas context.');
          return;
        }

        canvas.width = img.width;
        canvas.height = img.height;
        ctx.drawImage(img, 0, 0, img.width, img.height);
        
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const code = jsQR(imageData.data, imageData.width, imageData.height, { inversionAttempts: 'dontInvert' });

        if (code) {
          this.checkIn(code.data);
        } else {
          this.isScanning.set(false);
          this.errorMessage.set('No QR code found in the image. Please try again with a clearer image.');
        }
      };
      img.onerror = () => {
        this.isScanning.set(false);
        this.errorMessage.set('Failed to load the image.');
      };
      img.src = e.target?.result as string;
    };
    reader.readAsDataURL(file);
  }

  private checkIn(qrHash: string) {
    const apiUrl = environment.apiUrl;
    this.http.post<any>(`${apiUrl}/booking/checkin`, { qrHash }).subscribe({
      next: (res) => {
        this.isScanning.set(false);
        this.successMessage.set(`Checked in successfully! Booking ID: #${res.booking_Id || res.Booking_Id}`);
        setTimeout(() => {
          this.successMessage.set(null);
        }, 3000);
      },
      error: (err) => {
        this.isScanning.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to check in. Please try again.');
      }
    });
  }
}
