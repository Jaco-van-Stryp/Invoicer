export * from './aPI.service';
import { APIService } from './aPI.service';
export * from './auth.service';
import { AuthService } from './auth.service';
export * from './client.service';
import { ClientService } from './client.service';
export * from './company.service';
import { CompanyService } from './company.service';
export * from './estimate.service';
import { EstimateService } from './estimate.service';
export * from './file.service';
import { FileService } from './file.service';
export * from './invoice.service';
import { InvoiceService } from './invoice.service';
export * from './payment.service';
import { PaymentService } from './payment.service';
export * from './product.service';
import { ProductService } from './product.service';
export const APIS = [
  APIService,
  AuthService,
  ClientService,
  CompanyService,
  EstimateService,
  FileService,
  InvoiceService,
  PaymentService,
  ProductService,
];
