// Funkcije za rad s modalnim prozorima
function openCreateMecModal() {
    document.getElementById('createMecModal').classList.remove('hidden');
    if (typeof feather !== 'undefined') {
        feather.replace(); // Re-initialize icons in the modal
    }

    // Set default date to tomorrow
    const dateInput = document.querySelector('#createMecModal input[name="DatumVrijeme"]');
    if (dateInput) {
        const tomorrow = new Date();
        tomorrow.setDate(tomorrow.getDate() + 1);
        const tomorrowYear = tomorrow.getFullYear();
        const tomorrowMonth = (tomorrow.getMonth() + 1).toString().padStart(2, '0');
        const tomorrowDay = tomorrow.getDate().toString().padStart(2, '0');
        dateInput.value = `${tomorrowYear}-${tomorrowMonth}-${tomorrowDay}`;
        dateInput.min = new Date().toISOString().split('T')[0]; // Set min date to today
    }

    // Set default time to current time rounded to next half hour
    const timeSelect = document.querySelector('#createMecModal select[name="DatumVrijeme_Time"]');
    if (timeSelect) {
        const now = new Date();
        now.setMinutes(now.getMinutes() + 30);
        const roundedMinutes = now.getMinutes() < 30 ? 30 : 0;
        const hours = now.getHours() + (now.getMinutes() >= 30 ? 1 : 0);
        const hoursAdjusted = hours % 24;
        const timeValue = `${hoursAdjusted.toString().padStart(2, '0')}:${roundedMinutes.toString().padStart(2, '0')}`;

        for (let i = 0; i < timeSelect.options.length; i++) {
            if (timeSelect.options[i].value === timeValue) {
                timeSelect.selectedIndex = i;
                break;
            }
        }
    }
}

function closeCreateMecModal() {
    document.getElementById('createMecModal').classList.add('hidden');
}

// Inicijalizacija na učitavanje stranice
document.addEventListener('DOMContentLoaded', function () {
    // Inicijaliziraj feather ikone
    if (typeof feather !== 'undefined') {
        feather.replace();
    }
});
// Notification dropdown toggle
document.addEventListener('DOMContentLoaded', function () {
    const notificationButton = document.getElementById('notificationButton');
    const notificationDropdown = document.getElementById('notificationDropdown');

    if (notificationButton && notificationDropdown) {
        notificationButton.addEventListener('click', function () {
            notificationDropdown.classList.toggle('hidden');
        });

        // Close when clicking outside
        document.addEventListener('click', function (event) {
            if (!notificationButton.contains(event.target) &&
                !notificationDropdown.contains(event.target)) {
                notificationDropdown.classList.add('hidden');
            }
        });

        // Mark notifications as read when clicked
        const notificationLinks = notificationDropdown.querySelectorAll('a[data-notification-id]');
        notificationLinks.forEach(link => {
            link.addEventListener('click', function () {
                const notificationId = this.getAttribute('data-notification-id');
                fetch(`/Notifikacija/OznaciKaoProcitano/${notificationId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                });
            });
        });
    }
});
