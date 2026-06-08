import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  AddManualItemRequest,
  CreateShoppingListRequest,
  CreateShoppingListResponse,
  PurchaseWithExpenseRequest,
  ShoppingListDetailsDto,
  ShoppingListDto,
} from './shopping-lists.dto';

import { environment } from '../../environments/environment';

const API_BASE_URL = environment.apiBaseUrl;

@Injectable({ providedIn: 'root' })
export class ShoppingListsClient {
  private readonly http = inject(HttpClient);

  list(): Observable<ShoppingListDto[]> {
    return this.http.get<ShoppingListDto[]>(`${API_BASE_URL}/api/shopping-lists`);
  }

  get(id: string): Observable<ShoppingListDetailsDto> {
    return this.http.get<ShoppingListDetailsDto>(`${API_BASE_URL}/api/shopping-lists/${id}`);
  }

  create(payload: CreateShoppingListRequest): Observable<CreateShoppingListResponse> {
    return this.http.post<CreateShoppingListResponse>(`${API_BASE_URL}/api/shopping-lists`, payload);
  }

  addManualItem(shoppingListId: string, payload: AddManualItemRequest): Observable<void> {
    return this.http.post<void>(
      `${API_BASE_URL}/api/shopping-lists/${shoppingListId}/items/manual`,
      payload,
    );
  }

  markPending(shoppingListId: string, itemId: string): Observable<void> {
    return this.http.post<void>(
      `${API_BASE_URL}/api/shopping-lists/${shoppingListId}/items/${itemId}/pending`,
      null,
    );
  }

  purchaseWithExpense(
    shoppingListId: string,
    itemId: string,
    payload: PurchaseWithExpenseRequest,
  ): Observable<void> {
    return this.http.post<void>(
      `${API_BASE_URL}/api/shopping-lists/${shoppingListId}/items/${itemId}/purchase-with-expense`,
      payload,
    );
  }

  deleteItem(shoppingListId: string, itemId: string): Observable<void> {
    return this.http.delete<void>(
      `${API_BASE_URL}/api/shopping-lists/${shoppingListId}/items/${itemId}`,
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/api/shopping-lists/${id}`);
  }
}
