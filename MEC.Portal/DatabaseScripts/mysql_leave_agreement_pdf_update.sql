ALTER TABLE `leave_agreement`
    ADD COLUMN `agreement_pdf_file_name` VARCHAR(255) NULL,
    ADD COLUMN `agreement_pdf_original_file_name` VARCHAR(255) NULL,
    ADD COLUMN `agreement_pdf_content_type` VARCHAR(100) NULL,
    ADD COLUMN `agreement_pdf_size_bytes` BIGINT NULL,
    ADD COLUMN `agreement_pdf_uploaded_at` DATETIME(6) NULL;
