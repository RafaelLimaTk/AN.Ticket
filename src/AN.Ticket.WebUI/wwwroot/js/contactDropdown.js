$(document).ready(function () {
    $('#contactSearch').on('input', function () {
        var filter = $(this).val().toLowerCase();
        $('#contactDropdown .dropdown-item').each(function () {
            var text = $(this).text().toLowerCase();
            $(this).toggle(text.includes(filter));
        });
    });

    $('#contactDropdown .dropdown-item').on('click', function (e) {
        e.preventDefault();

        var selectedContactId = $(this).data('contact-id');
        var selectedContactName = $(this).data('contact-name');

        $('#contactInput').val(selectedContactName).focus();
        $('#selectedContactName').val(selectedContactName);
        var inputElement = $('#contactInput').get(0);
        inputElement.setSelectionRange(selectedContactName.length, selectedContactName.length);
        $('#contactInput').trigger('input');
        $('#selectedContactId').val(selectedContactId);

        loadContactDetails(selectedContactId);
    });

    $('#contactInput').on('input', function () {
        var currentValue = $(this).val().trim();
        $('#selectedContactName').val(currentValue);

        if (currentValue === '') {
            $('#selectedContactId').val('');
            loadContactDetails('00000000-0000-0000-0000-000000000000');
        }
    });

    function loadContactDetails(contactId) {
        $.ajax({
            url: '/Ticket/GetContactDetails',
            type: 'GET',
            data: { contactId: contactId },
            success: function (data) {
                $('#contactDetailsContainer').html(data);
            },
            error: function (xhr, status, error) {
                console.error('Erro ao carregar os detalhes do contato:', error);
            }
        });
    }
});
