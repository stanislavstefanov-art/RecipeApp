import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { ThemeService, Theme } from '../../core/theme.service';
import { LanguageService } from '../../core/language.service';

interface ThemeOption {
  id: Theme;
  labelKey: string;
  swatches: [string, string, string];
}

@Component({
  selector: 'app-appearance-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule],
  templateUrl: './appearance-settings.html',
})
export class AppearanceSettings {
  protected readonly themeService = inject(ThemeService);
  protected readonly langService = inject(LanguageService);

  protected readonly themes: ThemeOption[] = [
    { id: 'warm',   labelKey: 'settings.themes.warm',   swatches: ['#451a03', '#f59e0b', '#fffbeb'] },
    { id: 'fresh',  labelKey: 'settings.themes.fresh',  swatches: ['#022c22', '#10b981', '#ecfdf5'] },
    { id: 'indigo', labelKey: 'settings.themes.indigo', swatches: ['#1e1b4b', '#6366f1', '#eef2ff'] },
  ];

  protected readonly languages = [
    { code: 'bg', label: 'Български' },
    { code: 'en', label: 'English' },
  ];
}
