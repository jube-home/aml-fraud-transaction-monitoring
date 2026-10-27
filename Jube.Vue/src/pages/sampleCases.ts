/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

export type Risk = 'low' | 'medium' | 'high'

export interface SampleCase {
  id: string
  customer: string
  amount: number
  risk: Risk
  opened: string
}

export const sampleCases: SampleCase[] = [
  {
    id: 'C-1001',
    customer: 'Northwind Traders',
    amount: 12500.5,
    risk: 'high',
    opened: '2026-10-01',
  },
  { id: 'C-1002', customer: 'Contoso Payments', amount: 830, risk: 'low', opened: '2026-10-02' },
  {
    id: 'C-1003',
    customer: 'Fabrikam Exports',
    amount: 99999.99,
    risk: 'high',
    opened: '2026-10-03',
  },
  { id: 'C-1004', customer: 'Adventure Works', amount: 4120, risk: 'medium', opened: '2026-10-04' },
  { id: 'C-1005', customer: 'Tailspin Toys', amount: 150, risk: 'low', opened: '2026-10-05' },
  {
    id: 'C-1006',
    customer: 'Wide World Importers',
    amount: 28750,
    risk: 'medium',
    opened: '2026-10-06',
  },
  { id: 'C-1007', customer: 'Litware Holdings', amount: 61000, risk: 'high', opened: '2026-10-07' },
  { id: 'C-1008', customer: 'Proseware Retail', amount: 975.25, risk: 'low', opened: '2026-10-08' },
  {
    id: 'C-1009',
    customer: 'Northwind Logistics',
    amount: 7300,
    risk: 'medium',
    opened: '2026-10-09',
  },
  { id: 'C-1010', customer: 'Coho Vineyard', amount: 2210, risk: 'low', opened: '2026-10-10' },
]
