import { Inject, Optional, Pipe, PipeTransform } from '@angular/core';
import { BASE_PATH } from '../api/variables';

@Pipe({
  name: 'fileUrl',
})
export class FileUrlPipe implements PipeTransform {
  private readonly apiUrl: string;

  constructor(@Optional() @Inject(BASE_PATH) basePath: string | string[] | null) {
    this.apiUrl = Array.isArray(basePath) ? basePath[0] : (basePath ?? '');
  }

  transform(value: string | null | undefined): string | null {
    if (!value) return null;
    if (value.startsWith('data:') || value.startsWith('http')) return value;
    return `${this.apiUrl}/api/file/download/${value}`;
  }
}
