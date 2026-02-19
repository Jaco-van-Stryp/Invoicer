import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PublicInvoiceView } from './public-invoice-view';

describe('PublicInvoiceView', () => {
  let component: PublicInvoiceView;
  let fixture: ComponentFixture<PublicInvoiceView>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PublicInvoiceView],
    }).compileComponents();

    fixture = TestBed.createComponent(PublicInvoiceView);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
