import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, SiteDto } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class SiteService {
  private readonly baseUrl = `${environment.apiUrl}/sites`;

  constructor(private http: HttpClient) {}

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
