$(document).ready(function () {
    $('#userContactDropdown').on('change', function () {
        var selectedUserId = $(this).val();
        var selectedOption = this.options[this.selectedIndex];
        var type = selectedOption.getAttribute('data-type');

        if (selectedUserId) {
            loadUserDetails(selectedUserId, type);
        } else {
            loadUserDetails('00000000-0000-0000-0000-000000000000', type);
        }
    });

    function loadUserDetails(userId, type) {
        $.ajax({
            url: '/Asset/GetUserDetails',
            type: 'GET',
            data: { userId: userId, type: type },
            success: function (data) {
                $('#userDetailsContainer').html(data);
            },
            error: function (xhr, status, error) {
                console.error('Erro ao carregar os detalhes do usuário:', error);
            }
        });
    }

    document.getElementById('userContactDropdown').addEventListener('change', function () {
        var selectedOption = this.options[this.selectedIndex];
        var type = selectedOption.getAttribute('data-type');
        document.getElementById('userContactType').value = type;
    });
});
