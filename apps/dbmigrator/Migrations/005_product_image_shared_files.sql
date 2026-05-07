DROP INDEX IF EXISTS ux_product_images_stored_file_id;

CREATE UNIQUE INDEX IF NOT EXISTS ux_product_images_product_id_stored_file_id
    ON product_images (product_id, stored_file_id);
