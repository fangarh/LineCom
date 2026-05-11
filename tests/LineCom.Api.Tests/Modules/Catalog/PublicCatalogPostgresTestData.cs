using Dapper;
using Npgsql;

namespace LineCom.Api.Tests.Modules.Catalog;

internal static class PublicCatalogPostgresTestData
{
    public static async Task SeedAsync(NpgsqlConnection connection)
    {
        await connection.ExecuteAsync(
            """
            TRUNCATE product_attribute_values, attribute_value_aliases, attribute_options,
                category_attributes, product_images, products, brands, categories, stored_files
            RESTART IDENTITY CASCADE;

            INSERT INTO stored_files (id, storage_key, original_file_name, content_type, size_bytes, checksum, purpose)
            VALUES
                ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'storage/products/cable.jpg', 'cable.jpg', 'image/jpeg', 10, 'checksum-product', 'product_image'),
                ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'storage/brands/linecom.jpg', 'linecom.jpg', 'image/jpeg', 10, 'checksum-brand', 'brand_logo');

            INSERT INTO categories (id, name, slug, h1, description, seo_title, seo_description, sort_order)
            VALUES
                ('11111111-1111-1111-1111-111111111111', 'Cable', 'cable', 'Cable', 'Cable category', 'Cable buy', 'Cable catalog', 10),
                ('22222222-2222-2222-2222-222222222222', 'Hidden', 'hidden-category', 'Hidden', NULL, NULL, NULL, 20);

            UPDATE categories SET is_active = false WHERE id = '22222222-2222-2222-2222-222222222222';

            INSERT INTO brands (id, name, slug, logo_file_id)
            VALUES ('33333333-3333-3333-3333-333333333333', 'LineCom', 'linecom', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');

            INSERT INTO products (id, primary_category_id, brand_id, name, slug, sku, availability_status, sale_unit, unit_quantity, publish_status, seo_title, seo_description, sort_order)
            VALUES
                ('44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', '33333333-3333-3333-3333-333333333333', 'Cable UTP Cat 5e', 'u-utp-cat-5e', 'LC-001', 'in_stock', 'coil', '305 m', 'published', 'Cable UTP Cat 5e', 'Buy Cable UTP Cat 5e', 10),
                ('55555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111', NULL, 'Draft product', 'draft-product', 'LC-DRAFT', 'in_stock', 'coil', '305 m', 'draft', NULL, NULL, 20);

            INSERT INTO product_images (product_id, stored_file_id, alt, is_main)
            VALUES ('44444444-4444-4444-4444-444444444444', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Cable image', true);

            INSERT INTO category_attributes (id, category_id, name, code, type, is_filterable, is_visible_in_product, sort_order)
            VALUES ('66666666-6666-6666-6666-666666666666', '11111111-1111-1111-1111-111111111111', 'Conductor material', 'conductor-material', 'select', true, true, 10);

            INSERT INTO attribute_options (id, attribute_id, value, slug, normalized_value, sort_order)
            VALUES ('77777777-7777-7777-7777-777777777777', '66666666-6666-6666-6666-666666666666', 'CU', 'cu', 'cu', 10);

            INSERT INTO product_attribute_values (product_id, attribute_id, attribute_option_id, normalized_value)
            VALUES ('44444444-4444-4444-4444-444444444444', '66666666-6666-6666-6666-666666666666', '77777777-7777-7777-7777-777777777777', 'cu');
            """);
    }
}
