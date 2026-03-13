-- Пример SQL-дампа производителей для модуля «Производители».
-- Формат: INSERT INTO Manufacturers (Id, Prefix, Name, Contact, Description, Address, Website, Rating, ExternalId, ExistName, ExistId, Domain, TecdocSupplierId, MarketPrefix, IsDeleted)
-- Id — char(36) UUID, Prefix — уникальный суффикс артикула (после '=').

INSERT INTO Manufacturers (Id, Prefix, Name, Contact, Description, Address, Website, Rating, ExternalId, ExistName, ExistId, Domain, TecdocSupplierId, MarketPrefix, IsDeleted) VALUES
('a0000001-0000-0000-0000-000000000001', 'NKM', 'Nokian Tyres', NULL, 'Финский производитель шин', 'Финляндия, Нокиа', 'https://www.nokiantyres.com', 5, 1001, 'Nokian', 101, 'nokiantyres.com', 40, 'NKM', 0),
('a0000002-0000-0000-0000-000000000002', 'BSH', 'Bosch', NULL, 'Автокомпоненты Bosch', 'Германия, Штутгарт', 'https://www.bosch.com', 5, 1002, 'Bosch', 102, 'bosch.com', 58, 'BSH', 0),
('a0000003-0000-0000-0000-000000000003', 'DNP', 'Denso', NULL, 'Японский производитель автокомпонентов', 'Япония, Кария', 'https://www.denso.com', 4, 1003, 'Denso', 103, 'denso.com', 1028, 'DNP', 0),
('a0000004-0000-0000-0000-000000000004', 'FLT', 'Filtron', NULL, 'Фильтры Filtron', 'Польша, Гостынь', 'https://www.filtron.eu', 4, 1004, 'Filtron', 104, 'filtron.eu', 589, 'FLT', 0),
('a0000005-0000-0000-0000-000000000005', 'MBL', 'Mobil', NULL, 'Моторные масла Mobil', 'США, Ирвинг', 'https://www.mobil.com', 5, 1005, 'Mobil', 105, 'mobil.com', NULL, 'MBL', 0),
('a0000006-0000-0000-0000-000000000006', 'KYB', 'KYB (Kayaba)', NULL, 'Амортизаторы KYB', 'Япония, Токио', 'https://www.kyb.com', 4, 1006, 'KYB', 106, 'kyb.com', 536, 'KYB', 0),
('a0000007-0000-0000-0000-000000000007', 'LUK', 'LuK (Schaeffler)', NULL, 'Сцепления и маховики LuK', 'Германия, Бюль', 'https://www.schaeffler.com', 5, 1007, 'LuK', 107, 'schaeffler.com', 640, 'LUK', 0),
('a0000008-0000-0000-0000-000000000008', 'NGK', 'NGK Spark Plug', NULL, 'Свечи зажигания NGK', 'Япония, Нагоя', 'https://www.ngk.com', 5, 1008, 'NGK', 108, 'ngk.com', 760, 'NGK', 0),
('a0000009-0000-0000-0000-000000000009', 'SNR', 'SNR (NTN-SNR)', NULL, 'Подшипники SNR', 'Франция, Анси', 'https://www.ntn-snr.com', 4, 1009, 'SNR', 109, 'ntn-snr.com', 1111, 'SNR', 0),
('a0000010-0000-0000-0000-000000000010', 'SKF', 'SKF', NULL, 'Подшипники и уплотнения SKF', 'Швеция, Гётеборг', 'https://www.skf.com', 5, 1010, 'SKF', 110, 'skf.com', 1087, 'SKF', 0);
