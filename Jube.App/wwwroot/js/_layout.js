function onSelect(e) {
    const dataItem = this.dataItem(e.item.index());

    $.ajax({
        url: 'api/UserInTenant',
        type: 'PUT',
        data: {
            tenantRegistryId: dataItem.id
        },
        success: function () {
            window.location.reload(false);
        }
    });
}

$(document).ready(function () {
    $("#menu").kendoMenu();
    $("#Tenant").kendoAutoComplete({
        dataTextField: 'name',
        dataValueField: 'id',
        filter: 'contains',
        minLength: 3,
        select: onSelect,
        dataSource: {
            type: 'json',
            serverFiltering: true,
            transport: {
                read: 'api/TenantRegistry/ByFilter'
            }
        }
    });
});