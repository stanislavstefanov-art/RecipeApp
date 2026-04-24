import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { MealPlanEntryAssignmentDto } from '../../api/meal-plans.dto';
import { MealPlansDetails } from './meal-plans-details';

const STUB_ASSIGNMENT: MealPlanEntryAssignmentDto = {
  personId: 'person-1',
  personName: 'Alice',
  assignedRecipeId: 'recipe-1',
  assignedRecipeName: 'Pasta',
  recipeVariationId: null,
  recipeVariationName: null,
  portionMultiplier: 1,
  notes: null,
};

@Component({
  standalone: true,
  imports: [MealPlansDetails],
  template: '<app-meal-plans-details id="plan-1" />',
})
class TestHost {}

describe('MealPlansDetails — isEditing signal logic', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHost],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('isEditing returns false initially for any entry/person', () => {
    const fixture = TestBed.createComponent(TestHost);
    const details = fixture.debugElement.children[0].componentInstance as MealPlansDetails;
    expect((details as unknown as { isEditing: (e: string, p: string) => boolean }).isEditing('entry-1', 'person-1')).toBe(false);
  });

  it('isEditing returns true after onOpenEdit for the matching entry and person', () => {
    const fixture = TestBed.createComponent(TestHost);
    const details = fixture.debugElement.children[0].componentInstance as MealPlansDetails;
    const raw = details as unknown as {
      isEditing: (e: string, p: string) => boolean;
      onOpenEdit: (entryId: string, assignment: MealPlanEntryAssignmentDto) => void;
    };

    raw.onOpenEdit('entry-1', STUB_ASSIGNMENT);

    expect(raw.isEditing('entry-1', 'person-1')).toBe(true);
  });

  it('isEditing returns false for a different entry id', () => {
    const fixture = TestBed.createComponent(TestHost);
    const details = fixture.debugElement.children[0].componentInstance as MealPlansDetails;
    const raw = details as unknown as {
      isEditing: (e: string, p: string) => boolean;
      onOpenEdit: (entryId: string, assignment: MealPlanEntryAssignmentDto) => void;
    };

    raw.onOpenEdit('entry-1', STUB_ASSIGNMENT);

    expect(raw.isEditing('entry-2', 'person-1')).toBe(false);
  });

  it('isEditing returns false for a different person id', () => {
    const fixture = TestBed.createComponent(TestHost);
    const details = fixture.debugElement.children[0].componentInstance as MealPlansDetails;
    const raw = details as unknown as {
      isEditing: (e: string, p: string) => boolean;
      onOpenEdit: (entryId: string, assignment: MealPlanEntryAssignmentDto) => void;
    };

    raw.onOpenEdit('entry-1', STUB_ASSIGNMENT);

    expect(raw.isEditing('entry-1', 'person-2')).toBe(false);
  });

  it('isEditing returns false after onCancelEdit', () => {
    const fixture = TestBed.createComponent(TestHost);
    const details = fixture.debugElement.children[0].componentInstance as MealPlansDetails;
    const raw = details as unknown as {
      isEditing: (e: string, p: string) => boolean;
      onOpenEdit: (entryId: string, assignment: MealPlanEntryAssignmentDto) => void;
      onCancelEdit: () => void;
    };

    raw.onOpenEdit('entry-1', STUB_ASSIGNMENT);
    raw.onCancelEdit();

    expect(raw.isEditing('entry-1', 'person-1')).toBe(false);
  });
});
