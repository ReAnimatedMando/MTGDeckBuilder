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
        const popupWidth = 223;
        const popupHeight = 310;

        let left = e.clientX + offsetX;
        let top = e.clientY + offsetY;

        if (left + popupWidth > window.innerWidth) {
            left = e.clientX - popupWidth - offsetX;
        }

        if (top + popupHeight > window.innerHeight) {
            top = window.innerHeight - popupHeight - 10;
        }

        if (top < 10) {
            top = 10;
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
