-- 1. حذف الفهرس القديم (لأنه يعتمد على العمود)
DROP INDEX [IX_GraduateApplicationId] ON [dbo].[PaymentVoucher];
GO

-- 2. تعديل العمود ليقبل القيم الفارغة (NULL)
ALTER TABLE [dbo].[PaymentVoucher] 
ALTER COLUMN [GraduateApplicationId] INT NULL;
GO

-- 3. إعادة إنشاء الفهرس (اختياري ولكنه جيد للأداء)
CREATE NONCLUSTERED INDEX [IX_GraduateApplicationId] 
ON [dbo].[PaymentVoucher]([GraduateApplicationId] ASC);
GO