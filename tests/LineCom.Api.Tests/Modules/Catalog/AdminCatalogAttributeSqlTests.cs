using LineCom.Api.Modules.Catalog.Repositories;

namespace LineCom.Api.Tests.Modules.Catalog;

public sealed class AdminCatalogAttributeSqlTests
{
    [Fact]
    public void ListAttributes_LoadsCategoryAttributesAndSelectOptions()
    {
        Assert.Contains("FROM category_attributes attribute", AdminCatalogAttributeSql.ListAttributes);
        Assert.Contains("COUNT(value.id)::int AS \"ProductValuesCount\"", AdminCatalogAttributeSql.ListAttributes);
        Assert.Contains("FROM attribute_options option", AdminCatalogAttributeSql.ListOptions);
        Assert.Contains("value.attribute_option_id = option.id", AdminCatalogAttributeSql.ListOptions);
        Assert.Contains("attribute.type = 'select'", AdminCatalogAttributeSql.ListOptions);
        Assert.Contains("ORDER BY attribute.sort_order, attribute.name, attribute.code", AdminCatalogAttributeSql.ListAttributes);
        Assert.Contains("ORDER BY option.attribute_id, option.sort_order, option.value", AdminCatalogAttributeSql.ListOptions);
    }

    [Fact]
    public void AttributeDelete_PhysicallyDeletesOnlyUnusedAttributes()
    {
        Assert.Contains("DELETE FROM category_attributes", AdminCatalogAttributeSql.DeleteAttribute);
        Assert.Contains("NOT EXISTS", AdminCatalogAttributeSql.DeleteAttribute);
        Assert.Contains("FROM product_attribute_values value", AdminCatalogAttributeSql.DeleteAttribute);
        Assert.Contains("value.attribute_id = attribute.id", AdminCatalogAttributeSql.DeleteAttribute);
    }

    [Fact]
    public void OptionDelete_PhysicallyDeletesOnlyUnusedOptions()
    {
        Assert.Contains("DELETE FROM attribute_options", AdminCatalogAttributeSql.DeleteOption);
        Assert.Contains("DELETE FROM attribute_value_aliases value_alias", AdminCatalogAttributeSql.DeleteOption);
        Assert.Contains("value_alias.option_id = option_to_delete.id", AdminCatalogAttributeSql.DeleteOption);
        Assert.Contains("SELECT COUNT(*)::int FROM deleted_option", AdminCatalogAttributeSql.DeleteOption);
        Assert.Contains("NOT EXISTS", AdminCatalogAttributeSql.DeleteOption);
        Assert.Contains("value.attribute_option_id = option.id", AdminCatalogAttributeSql.DeleteOption);
    }

    [Fact]
    public void InheritFromParent_CopiesMissingAttributesAndOptionsByCode()
    {
        Assert.Contains("parent_category.parent_id", AdminCatalogAttributeSql.InheritMissingAttributes);
        Assert.Contains("child_attribute.code = parent_attribute.code", AdminCatalogAttributeSql.InheritMissingAttributes);
        Assert.Contains("parent_attribute.id AS \"ParentAttributeId\"", AdminCatalogAttributeSql.InheritMissingAttributes);
        Assert.Contains("inserted_attributes.child_attribute_id AS \"ChildAttributeId\"", AdminCatalogAttributeSql.InheritMissingAttributes);
        Assert.Contains("WHERE NOT EXISTS", AdminCatalogAttributeSql.InheritMissingAttributes);
        Assert.Contains("INSERT INTO attribute_options", AdminCatalogAttributeSql.InheritOptionsForCopiedAttributes);
        Assert.Contains("AS copied_attributes(child_attribute_id, parent_attribute_id)", AdminCatalogAttributeSql.InheritOptionsForCopiedAttributes);
        Assert.Contains("copied_attributes.child_attribute_id", AdminCatalogAttributeSql.InheritOptionsForCopiedAttributes);
        Assert.Contains("copied_attributes.parent_attribute_id", AdminCatalogAttributeSql.InheritOptionsForCopiedAttributes);
    }
}
