$(document).ready(function () {
    $(".nav-link").on("click", function () {
        $(".nav-link").removeClass("active");
        $(this).addClass("active");

        $("#conversa-content, #resolucao-content, #anexo-content, #atividade-content, #aprovacao-content, #ativos-content").hide();

        var tabId = $(this).attr("id");
        if (tabId === "tab1") {
            $("#conversa-content").show();
        } else if (tabId === "tab2") {
            $("#resolucao-content").show();
        } else if (tabId === "tab3") {
            $("#anexo-content").show();
        } else if (tabId === "tab4") {
            $("#atividade-content").show();
        } else if (tabId === "tab5") {
            $("#aprovacao-content").show();
        } else if (tabId === "tab6") {
            $("#ativos-content").show();
        }
    });

    $("#showResponseForm").on("click", function () {
        $("#responseForm").show();
    });

    $("#hideResponseForm").on("click", function () {
        $("#responseForm").hide();
    });
});

function handleFiles(files) {
    const fileInput = document.getElementById('attachments');
    const dt = new DataTransfer();
    const fileList = document.getElementById('fileList');
    fileList.innerHTML = '';

    for (let i = 0; i < files.length; i++) {
        const file = files[i];

        dt.items.add(file);

        const fileItem = document.createElement('div');
        fileItem.className = 'p-2 m-1 bg-light border rounded d-flex align-items-center';
        fileItem.style.maxWidth = '200px';
        fileItem.innerHTML = `
            <i class="bi bi-file-earmark me-2"></i>
            <span class="text-truncate" style="max-width: 150px;">${file.name}</span>
            <button type="button" class="btn btn-sm btn-outline-danger ms-2" onclick="removeFile(${i})"><i class="bi bi-x-circle"></i></button>
        `;
        fileList.appendChild(fileItem);
    }

    fileInput.files = dt.files;
}

function removeFile(index) {
    const fileInput = document.getElementById('attachments');
    const dt = new DataTransfer();

    for (let i = 0; i < fileInput.files.length; i++) {
        if (i !== index) {
            dt.items.add(fileInput.files[i]);
        }
    }

    fileInput.files = dt.files;

    handleFiles(fileInput.files);
}

$('.expandable').each(function () {
    $(this).on('click', function () {
        var content = $(this).find('.message-body');
        var icon = $(this).find('.icon-expand i');
        if (content.css('display') === 'none') {
            content.css('display', 'block');
            icon.removeClass('bi-caret-right').addClass('bi-caret-down');
        } else {
            content.css('display', 'none');
            icon.removeClass('bi-caret-down').addClass('bi-caret-right');
        }
    });
});
