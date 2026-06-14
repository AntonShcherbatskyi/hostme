import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse, SiteDto } from '../models/api.models';
import { AppConfigService } from './app-config.service';

@Injectable({ providedIn: 'root' })
export class SiteService {
  private get baseUrl(): string {
    return `${this.config.apiUrl}/sites`;
  }

  constructor(private http: HttpClient, private config: AppConfigService) {}

  getSites(): Observable<ApiResponse<SiteDto[]>> {
    return this.http.get<ApiResponse<SiteDto[]>>(this.baseUrl);
  }

  uploadSite(name: string, file: File): Observable<ApiResponse<SiteDto>> {
    const formData = new FormData();
    formData.append('name', name);
    formData.append('file', file);
    return this.http.post<ApiResponse<SiteDto>>(`${this.baseUrl}/upload`, formData);
  }

  deleteSite(id: string): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }
}
