import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Theme, ThemeService } from '../../../core/theme.service';

interface ThemeOption {
  id: Theme;
  label: string;
  color: string;
}

@Component({
  selector: 'app-theme-switcher',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './theme-switcher.html',
})
export class ThemeSwitcher {
  protected readonly themeService = inject(ThemeService);

  protected readonly themes: ThemeOption[] = [
    { id: 'warm',   label: 'Warm & Earthy', color: '#b45309' },
    { id: 'fresh',  label: 'Fresh & Green',  color: '#047857' },
    { id: 'indigo', label: 'Indigo Modern',  color: '#4338ca' },
  ];
}
