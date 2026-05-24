import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { MeasurementUnitDto } from '../api/units.dto';

@Pipe({ name: 'unitName', standalone: true, pure: false })
export class UnitNamePipe implements PipeTransform {
  private readonly translate = inject(TranslateService);

  transform(unit: MeasurementUnitDto | string): string {
    const abbreviation = typeof unit === 'string' ? unit : unit.abbreviation;
    const fallback = typeof unit === 'string' ? unit : unit.name;
    const key = `enums.unit.${abbreviation}`;
    const result = this.translate.instant(key);
    return result === key ? fallback : result;
  }
}
