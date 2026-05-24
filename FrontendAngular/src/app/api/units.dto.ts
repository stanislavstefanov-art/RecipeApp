export interface MeasurementUnitDto {
  id: string;
  name: string;
  abbreviation: string;
}

export interface CreateUnitRequest {
  name: string;
  abbreviation: string;
}
