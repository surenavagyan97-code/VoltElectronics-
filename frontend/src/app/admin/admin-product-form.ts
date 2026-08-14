import { Component, ElementRef, OnInit, effect, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../core/api-client';
import { Category, ProductImage, ProductSpec } from '../core/api.types';
import { extractError } from '../core/cart-store';
import { I18nService, LANG_LABELS } from '../core/i18n.service';

/** Languages the form offers name translations for; the main Name field is the canonical English. */
const TRANSLATION_LANGS = ['hy', 'ru'] as const;

@Component({
  selector: 'app-admin-product-form',
  imports: [FormsModule, RouterLink],
  styles: `.input.invalid { border-color: #ff8a8a; }`,
  template: `
    <div style="max-width: 860px;">
      <a class="btn btn-ghost" style="padding: 0; margin-bottom: 12px;" routerLink="/admin/products">{{ i18n.t('admin.form.backToProducts') }}</a>
      <h2 style="margin-bottom: 22px;">{{ isNew() ? i18n.t('admin.form.addProduct') : i18n.t('admin.form.editProduct') }}</h2>

      <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 18px;">
        <div class="field" style="grid-column: span 2;"><label>{{ i18n.t('admin.form.productName') }} *</label>
          <input class="input" [class.invalid]="invalid(form.name)" [(ngModel)]="form.name" [placeholder]="i18n.t('admin.form.productNamePlaceholder')" /></div>
        @for (t of form.translations; track t.lang) {
          <div class="field"><label>{{ i18n.t('admin.form.productName') }} ({{ langLabels[t.lang] ?? t.lang }})</label>
            <input class="input" [(ngModel)]="t.name" [ngModelOptions]="{ standalone: true }"
                   [placeholder]="i18n.t('admin.form.translationPlaceholder')" /></div>
        }
        <div class="field"><label>{{ i18n.t('admin.form.sku') }} *</label>
          <input class="input" [class.invalid]="invalid(form.sku)" [(ngModel)]="form.sku" placeholder="VLT-LP-2201" /></div>
        <div class="field"><label>{{ i18n.t('admin.form.category') }} *</label>
          <select class="input" [class.invalid]="invalid(form.categoryId)" [(ngModel)]="form.categoryId">
            @for (cat of categories(); track cat.id) {
              <option [ngValue]="cat.id">{{ cat.name }}</option>
            }
          </select>
        </div>
        <div class="field"><label>{{ i18n.t('admin.form.price') }} *</label>
          <input class="input" [class.invalid]="invalid(form.price)" type="number" step="0.01" [(ngModel)]="form.price" placeholder="1499.00" /></div>
        <div class="field"><label>{{ i18n.t('admin.form.compareAtPrice') }}</label>
          <input class="input" type="number" step="0.01" [(ngModel)]="form.compareAtPrice" placeholder="1699.00" /></div>
        <div class="field"><label>{{ i18n.t('admin.form.stockQuantity') }}</label>
          <input class="input" type="number" [(ngModel)]="form.stock" placeholder="24" /></div>
        <div class="field"><label>{{ i18n.t('admin.form.badge') }}</label>
          <input class="input" [(ngModel)]="form.badge" [placeholder]="i18n.t('admin.form.badgePlaceholder')" /></div>
        <div class="field" style="grid-column: span 2;"><label>{{ i18n.t('admin.form.status') }}</label>
          <div class="seg" style="width: fit-content;">
            @for (s of statuses; track s.value) {
              <label class="seg-opt"><input type="radio" name="status" [value]="s.value" [(ngModel)]="form.status" />{{ s.label() }}</label>
            }
          </div>
        </div>
        <div class="field" style="grid-column: span 2;"><label>{{ i18n.t('admin.form.description') }}</label>
          <textarea class="input" [(ngModel)]="form.description" placeholder="A 15-inch aluminum-unibody workstation built for sustained performance..."></textarea></div>

        @if (!isNew()) {
          <div class="field" style="grid-column: span 2;">
            <label>{{ i18n.t('admin.form.productImages') }}</label>
            <div class="row" style="gap: 12px; flex-wrap: wrap;">
              @for (img of images(); track img.id) {
                <div style="position: relative;">
                  <div class="ph" style="width: 110px; height: 110px;"><img [src]="img.thumbUrl" alt="" /></div>
                  <button class="btn btn-icon btn-secondary" style="position: absolute; top: 4px; right: 4px; width: 24px; height: 24px; background: var(--color-bg);"
                          (click)="removeImage(img)" aria-label="Remove image">×</button>
                </div>
              }
              <label class="ph" style="width: 110px; height: 110px; cursor: pointer;">
                {{ i18n.t('admin.form.uploadImage') }}
                <input type="file" accept="image/*" style="display: none;" (change)="uploadImage($event)" />
              </label>
            </div>
          </div>
        } @else {
          <p class="text-muted" style="grid-column: span 2; font-size: 13px; margin: 0;">{{ i18n.t('admin.form.saveProductFirst') }}</p>
        }

        <div class="field" style="grid-column: span 2;">
          <label>{{ i18n.t('admin.form.specifications') }}</label>
          <div class="col" style="gap: 8px;">
            @for (spec of form.specs; track $index) {
              <div class="row" style="gap: 8px;">
                <input class="input" [placeholder]="i18n.t('admin.form.specNamePlaceholder')" style="width: 200px;"
                       [(ngModel)]="spec.name" [ngModelOptions]="{ standalone: true }" />
                <input class="input" [placeholder]="i18n.t('admin.form.specValuePlaceholder')"
                       [(ngModel)]="spec.value" [ngModelOptions]="{ standalone: true }" />
                <button class="btn btn-icon btn-ghost" (click)="form.specs.splice($index, 1)" aria-label="Remove spec">×</button>
              </div>
            }
            <button class="btn btn-ghost" style="align-self: flex-start;" (click)="form.specs.push({ name: '', value: '' })">{{ i18n.t('admin.form.addSpec') }}</button>
          </div>
        </div>
      </div>

      @if (error(); as err) { <div #errorBox class="error-text" style="margin-top: 14px;">{{ err }}</div> }

      <div class="row" style="gap: 10px; margin-top: 26px;">
        <button class="btn btn-primary" [disabled]="busy()" (click)="save()">{{ busy() ? i18n.t('admin.form.saving') : i18n.t('admin.form.saveProduct') }}</button>
        <a class="btn btn-secondary" routerLink="/admin/products">{{ i18n.t('admin.form.discard') }}</a>
      </div>
    </div>
  `,
})
export class AdminProductFormPage implements OnInit {
  private api = inject(ApiClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  i18n = inject(I18nService);

  private id: number | null = null;
  private errorBox = viewChild<ElementRef<HTMLElement>>('errorBox');
  isNew = signal(true);
  categories = signal<Category[]>([]);
  images = signal<ProductImage[]>([]);
  busy = signal(false);
  error = signal<string | null>(null);
  submitted = signal(false);

  readonly statuses = [
    { value: 'Active', label: () => this.i18n.t('admin.form.statusActive') },
    { value: 'Draft', label: () => this.i18n.t('admin.form.statusDraft') },
    { value: 'Archived', label: () => this.i18n.t('admin.form.statusArchived') },
  ];

  readonly langLabels: Record<string, string> = LANG_LABELS;

  form: {
    name: string; sku: string; categoryId: number | null; description: string;
    price: number | null; compareAtPrice: number | null; stock: number;
    status: string; badge: string; specs: ProductSpec[];
    translations: { lang: string; name: string }[];
  } = {
    name: '', sku: '', categoryId: null, description: '',
    price: null, compareAtPrice: null, stock: 0, status: 'Active', badge: '',
    specs: [{ name: '', value: '' }],
    translations: TRANSLATION_LANGS.map((lang) => ({ lang, name: '' })),
  };

  async ngOnInit(): Promise<void> {
    this.categories.set(await firstValueFrom(this.api.adminGetCategories()));

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id = +idParam;
      this.isNew.set(false);
      const p = await firstValueFrom(this.api.adminGetProduct(this.id));
      this.form = {
        name: p.name, sku: p.sku, categoryId: p.categoryId, description: p.description,
        price: p.price, compareAtPrice: p.compareAtPrice, stock: p.stock,
        status: p.status, badge: p.badge ?? '',
        specs: p.specs.length ? p.specs.map((s) => ({ ...s })) : [{ name: '', value: '' }],
        translations: TRANSLATION_LANGS.map((lang) => ({
          lang,
          name: p.translations.find((t) => t.lang === lang)?.name ?? '',
        })),
      };
      this.images.set(p.images);
    } else if (this.categories().length) {
      this.form.categoryId = this.categories()[0].id;
    }
  }

  constructor() {
    // errorBox() only resolves once the @if block has rendered it, which happens on
    // some later change-detection pass — an effect re-fires once that query settles,
    // where a plain setTimeout after error.set() would still see it as undefined.
    effect(() => {
      if (this.error()) this.errorBox()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });
  }

  invalid(value: unknown): boolean {
    return this.submitted() && !value;
  }

  private showError(message: string): void {
    this.error.set(message);
  }

  async save(): Promise<void> {
    this.submitted.set(true);
    if (!this.form.name || !this.form.sku || !this.form.categoryId || !this.form.price) {
      this.showError(this.i18n.t('admin.form.requiredFieldsError'));
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    const request = {
      name: this.form.name,
      sku: this.form.sku,
      categoryId: this.form.categoryId,
      description: this.form.description,
      price: this.form.price,
      compareAtPrice: this.form.compareAtPrice || null,
      stock: this.form.stock || 0,
      status: this.form.status,
      badge: this.form.badge || null,
      specs: this.form.specs.filter((s) => s.name && s.value),
      translations: this.form.translations.filter((t) => t.name.trim()),
    };
    try {
      if (this.id === null) {
        const { id } = await firstValueFrom(this.api.adminCreateProduct(request));
        void this.router.navigate(['/admin/products', id]);
      } else {
        await firstValueFrom(this.api.adminUpdateProduct(this.id, request));
        void this.router.navigate(['/admin/products']);
      }
    } catch (e) {
      this.showError(extractError(e));
    } finally {
      this.busy.set(false);
    }
  }

  async uploadImage(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || this.id === null) return;
    try {
      const img = await firstValueFrom(this.api.adminUploadImage(this.id, file));
      this.images.update((imgs) => [...imgs, img]);
    } catch (e) {
      this.error.set(extractError(e));
    } finally {
      input.value = '';
    }
  }

  async removeImage(img: ProductImage): Promise<void> {
    if (this.id === null) return;
    try {
      await firstValueFrom(this.api.adminRemoveImage(this.id, img.id));
      this.images.update((imgs) => imgs.filter((i) => i.id !== img.id));
    } catch (e) {
      this.error.set(extractError(e));
    }
  }
}
