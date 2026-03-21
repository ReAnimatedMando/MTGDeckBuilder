document.addEventListener("DOMContentLoaded", function () {
    const popup = document.getElementById("cardPreviewPopup");
    const popupImage = document.getElementById("cardPreviewImage");
    const triggers = document.querySelectorAll(".card-preview-trigger");

    if (!popup || !popupImage || triggers.length === 0) {
        return;
    }

    function movePopup(e) {
        const offsetX = 20;
        const offsetY = 20;
        const padding = 10;

        let left = e.clientX + offsetX;
        let top = e.clientY + offsetY;

        const popupRect = popup.getBoundingClientRect();

        if (left + popupRect.width > window.innerWidth - padding) {
            left = e.clientX - popupRect.width - offsetX;
        }

        if (top + popupRect.height > window.innerHeight - padding) {
            top = window.innerHeight - popupRect.height - padding;
        }

        if (top < padding) {
            top = padding;
        }

        if (left < padding) {
            left = padding;
        }

        popup.style.left = left + "px";
        popup.style.top = top + "px";
    }

    triggers.forEach(trigger => {
        trigger.addEventListener("mouseenter", function (e) {
            const imageUrl = this.dataset.imageUrl;

            if (!imageUrl) {
                return;
            }

            popupImage.src = imageUrl;
            popupImage.alt = this.dataset.cardName || "Card Preview";
            popup.classList.remove("d-none");
            movePopup(e);
        });

        trigger.addEventListener("mousemove", function (e) {
            if (!popup.classList.contains("d-none")) {
                movePopup(e);
            }
        });

        trigger.addEventListener("mouseleave", function () {
            popup.classList.add("d-none");
            popupImage.src = "";
        });
    });
});
