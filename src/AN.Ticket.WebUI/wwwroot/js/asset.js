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

    function truncateFileName(fileName) {
        return fileName.length > 10 ? fileName.substring(0, 10) + "..." : fileName;
    }

    document.querySelector('input[type="file"]').addEventListener("change", function (event) {
        const fileList = Array.from(event.target.files);
        const filesContainer = document.getElementById("filesContainer");

        fileList.forEach(file => {
            if (!newFiles.some(f => f.name === file.name && f.size === file.size)) {
                newFiles.push(file);

                const cardDiv = document.createElement("div");
                cardDiv.classList.add("col-12", "col-md-6", "col-lg-4", "file-card", "mb-3");

                cardDiv.innerHTML = `
                    <div class="card">
                        <div class="card-body d-flex justify-content-between align-items-center">
                            <span title="${file.name}" class="file-name">${truncateFileName(file.name)}</span>
                            <div>
                                <button type="button" class="btn btn-sm btn-danger remove-button">
                                    <i class="bi bi-trash"></i>
                                </button>
                            </div>
                        </div>
                    </div>`;

                cardDiv.querySelector(".remove-button").addEventListener("click", () => {
                    const index = newFiles.indexOf(file);
                    if (index > -1) newFiles.splice(index, 1);
                    cardDiv.remove();
                });

                filesContainer.appendChild(cardDiv);
            }
        });
    });
});

let newFiles = [];
let filesToRemove = [];
function removeExistingFile(fileId) {
    filesToRemove.push(fileId);

    const hiddenInput = document.createElement("input");
    hiddenInput.type = "hidden";
    hiddenInput.name = "FilesToRemove[]";
    hiddenInput.value = fileId;
    document.querySelector("form").appendChild(hiddenInput);

    document.querySelector(`[data-file-id="${fileId}"]`).remove();
}