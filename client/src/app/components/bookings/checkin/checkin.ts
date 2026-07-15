import { Component, ElementRef, ViewChild, ChangeDetectionStrategy, signal, computed, OnDestroy } from '@angular/core';
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
export class CheckinComponent implements OnDestroy {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('canvas') canvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('videoElement') videoElement!: ElementRef<HTMLVideoElement>;

  public isDragging = signal(false);
  public isScanning = signal(false);
  public successMessage = signal<string | null>(null);
  public errorMessage = signal<string | null>(null);

  public isCameraActive = signal(false);
  public cameraError = signal<string | null>(null);
  public availableCameras = signal<MediaDeviceInfo[]>([]);
  public selectedCameraId = signal<string | null>(null);

  private stream: MediaStream | null = null;
  private animationFrameId: number | null = null;
  private isProcessingQR = false;
  private errorTimeoutId: any = null;
  public isErrorFadingOut = signal(false);

  private showError(message: string) {
    this.errorMessage.set(message);
    this.isErrorFadingOut.set(false);
    
    if (this.errorTimeoutId) {
      clearTimeout(this.errorTimeoutId);
    }
    this.errorTimeoutId = setTimeout(() => {
      this.isErrorFadingOut.set(true);
      setTimeout(() => {
        if (this.isErrorFadingOut()) {
          this.errorMessage.set(null);
          this.isErrorFadingOut.set(false);
        }
      }, 500); // 500ms fade out
    }, 3500); // Start fade out after 3.5s
  }

  constructor(private http: HttpClient) {}

  ngOnDestroy() {
    this.stopCamera();
  }

  public async startCamera() {
    this.cameraError.set(null);
    this.errorMessage.set(null);
    
    try {
      let constraints: MediaStreamConstraints = { video: { facingMode: 'environment' } };
      const targetDeviceId = this.selectedCameraId();
      
      if (targetDeviceId) {
        constraints = { video: { deviceId: { exact: targetDeviceId } } };
      }

      this.stream = await navigator.mediaDevices.getUserMedia(constraints);
      this.isCameraActive.set(true);
      
      if (this.availableCameras().length === 0) {
        const devices = await navigator.mediaDevices.enumerateDevices();
        const videoDevices = devices.filter(d => d.kind === 'videoinput');
        this.availableCameras.set(videoDevices);
        
        const activeTrack = this.stream.getVideoTracks()[0];
        if (activeTrack) {
           const activeSettings = activeTrack.getSettings();
           if (activeSettings.deviceId) {
              this.selectedCameraId.set(activeSettings.deviceId);
           }
        }
      }
      
      setTimeout(async () => {
        if (this.videoElement?.nativeElement) {
          const video = this.videoElement.nativeElement;
          video.srcObject = this.stream;
          video.setAttribute('playsinline', 'true');
          await video.play();
          this.scanFrame();
        }
      }, 0);
    } catch (err) {
      console.error('Error accessing camera:', err);
      this.cameraError.set('Could not access the camera. Please grant permissions and try again.');
    }
  }

  public stopCamera() {
    this.isCameraActive.set(false);
    
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }
    
    if (this.stream) {
      this.stream.getTracks().forEach(track => track.stop());
      this.stream = null;
    }
    
    if (this.videoElement?.nativeElement) {
      this.videoElement.nativeElement.srcObject = null;
    }
  }

  public onCameraSelect(event: Event) {
    const select = event.target as HTMLSelectElement;
    const deviceId = select.value;
    if (deviceId && deviceId !== this.selectedCameraId()) {
      this.selectedCameraId.set(deviceId);
      this.stopCamera();
      // small delay to ensure resources are freed
      setTimeout(() => {
        this.startCamera();
      }, 100);
    }
  }

  private scanFrame() {
    if (!this.isCameraActive()) return;

    const video = this.videoElement?.nativeElement;
    const canvasEl = this.canvas?.nativeElement;
    if (!video || !canvasEl) return;
    
    const ctx = canvasEl.getContext('2d', { willReadFrequently: true });

    if (video.readyState === video.HAVE_ENOUGH_DATA && ctx) {
      canvasEl.width = video.videoWidth;
      canvasEl.height = video.videoHeight;
      
      ctx.drawImage(video, 0, 0, canvasEl.width, canvasEl.height);
      const imageData = ctx.getImageData(0, 0, canvasEl.width, canvasEl.height);
      const code = jsQR(imageData.data, imageData.width, imageData.height, { inversionAttempts: 'dontInvert' });

      if (code && !this.isProcessingQR) {
        this.isProcessingQR = true;
        this.checkIn(code.data, true);
      }
    }

    this.animationFrameId = requestAnimationFrame(() => this.scanFrame());
  }

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

  private checkIn(qrHash: string, fromCamera = false) {
    const apiUrl = environment.apiUrl;
    this.http.post<any>(`${apiUrl}/booking/checkin`, { qrHash }).subscribe({
      next: (res) => {
        this.isScanning.set(false);
        this.isProcessingQR = false;
        
        if (fromCamera) {
          this.stopCamera();
        }

        this.successMessage.set(`Checked in successfully! Booking ID: #${res.booking_Id || res.Booking_Id}`);
        setTimeout(() => {
          this.successMessage.set(null);
        }, 3000);
      },
      error: (err) => {
        this.isScanning.set(false);
        this.showError(err.error?.message || 'Failed to check in. Please try again.');
        
        if (fromCamera) {
          // Pause before allowing another scan from camera to prevent spamming API on invalid QR
          setTimeout(() => {
            this.isProcessingQR = false;
          }, 2500);
        } else {
          this.isProcessingQR = false;
        }
      }
    });
  }
}
