export * from './auth.service';
import { AuthService } from './auth.service';
export * from './client.service';
import { ClientService } from './client.service';
export * from './company.service';
import { CompanyService } from './company.service';
export * from './file.service';
import { FileService } from './file.service';
export * from './invoice.service';
import { InvoiceService } from './invoice.service';
export * from './product.service';
import { ProductService } from './product.service';
export const APIS = [
  AuthService,
  ClientService,
  CompanyService,
  FileService,
  InvoiceService,
  ProductService,
];
