document.addEventListener("DOMContentLoaded", () => {
    const data = window.bookingData;
    const form = document.getElementById("bookingForm");
    if (!data || !form) return;

    const serviceInput = document.getElementById("serviceId");
    const barberInput = document.getElementById("barberId");
    const dateInput = document.getElementById("dateInput");
    const timeInput = document.getElementById("timeInput");
    const timeSlots = document.getElementById("timeSlots");
    const slotMessage = document.getElementById("slotMessage");
    const summaryService = document.getElementById("summaryService");
    const summaryBarber = document.getElementById("summaryBarber");
    const summaryDate = document.getElementById("summaryDate");
    const summaryTime = document.getElementById("summaryTime");
    const summaryPrice = document.getElementById("summaryPrice");

    const serviceById = id => data.services.find(x => Number(x.id) === Number(id));
    const barberById = id => data.barbers.find(x => Number(x.id) === Number(id));

    function refreshSummary() {
        const service = serviceById(serviceInput.value);
        const barber = barberById(barberInput.value);
        summaryService.textContent = service?.name ?? "—";
        summaryBarber.textContent = barber?.name ?? "—";
        summaryDate.textContent = dateInput.value
            ? new Date(dateInput.value + "T00:00:00").toLocaleDateString("bg-BG", { day:"2-digit", month:"2-digit", year:"numeric" })
            : "—";
        summaryTime.textContent = timeInput.value || "—";
        summaryPrice.textContent = service ? `${Number(service.price).toFixed(2)} €` : "—";
    }

    let requestCounter = 0;

    async function loadSlots() {
        const hasAll = Number(serviceInput.value) > 0 && Number(barberInput.value) > 0 && dateInput.value;
        timeInput.value = "";
        refreshSummary();

        if (!hasAll) {
            timeSlots.innerHTML = "";
            slotMessage.textContent = "Избери услуга, бръснар и дата, за да видиш свободните часове.";
            return;
        }

        // Ако потребителят смени избора бързо, отговорът от по-старата заявка се игнорира.
        const requestId = ++requestCounter;
        timeSlots.innerHTML = '<span class="slot-message">Зареждане...</span>';
        slotMessage.textContent = "";

        try {
            const url = `/Booking/AvailableTimes?barberId=${encodeURIComponent(barberInput.value)}&serviceId=${encodeURIComponent(serviceInput.value)}&date=${encodeURIComponent(dateInput.value)}`;
            const response = await fetch(url, { headers: { "Accept": "application/json" } });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const slots = await response.json();
            if (requestId !== requestCounter) return;
            timeSlots.innerHTML = "";

            if (!slots.length) {
                slotMessage.textContent = "Няма свободни часове за тази дата. Опитай с друга дата.";
                refreshSummary();
                return;
            }

            slotMessage.textContent = "";
            slots.forEach(time => {
                const button = document.createElement("button");
                button.type = "button";
                button.className = "time-slot";
                button.dataset.time = time;
                button.textContent = time;
                button.addEventListener("click", () => {
                    document.querySelectorAll(".time-slot").forEach(x => x.classList.remove("active"));
                    button.classList.add("active");
                    timeInput.value = time;
                    refreshSummary();
                });
                timeSlots.appendChild(button);
            });
        } catch {
            timeSlots.innerHTML = "";
            slotMessage.textContent = "Не успяхме да заредим часовете. Опитай отново.";
        }
    }

    document.querySelectorAll('input[name="serviceChoice"]').forEach(input => {
        input.addEventListener("change", () => {
            serviceInput.value = input.value;
            document.querySelectorAll('input[name="serviceChoice"]').forEach(x => x.closest(".choice-card").classList.remove("selected"));
            input.closest(".choice-card").classList.add("selected");
            loadSlots();
            refreshSummary();
        });
    });

    document.querySelectorAll('input[name="barberChoice"]').forEach(input => {
        input.addEventListener("change", () => {
            barberInput.value = input.value;
            document.querySelectorAll('input[name="barberChoice"]').forEach(x => x.closest(".choice-card").classList.remove("selected"));
            input.closest(".choice-card").classList.add("selected");
            loadSlots();
            refreshSummary();
        });
    });

    dateInput.addEventListener("change", loadSlots);
    document.querySelectorAll(".time-slot").forEach(button => {
        button.addEventListener("click", () => {
            document.querySelectorAll(".time-slot").forEach(x => x.classList.remove("active"));
            button.classList.add("active");
            timeInput.value = button.dataset.time;
            refreshSummary();
        });
    });

    // Връщане на формата след грешка от сървъра: запазваме избрания час, ако още е свободен.
    const previouslySelectedTime = timeInput.value;
    refreshSummary();

    if (Number(serviceInput.value) > 0 && Number(barberInput.value) > 0 && dateInput.value) {
        loadSlots().then(() => {
            if (!previouslySelectedTime) return;
            const match = [...document.querySelectorAll(".time-slot")].find(b => b.dataset.time === previouslySelectedTime);
            if (match) match.click();
        });
    }
});
