"use strict";

const state = { rooms: [], services: [], editingRoomId: null };
const money = new Intl.NumberFormat("en-UA", { style: "currency", currency: "UAH", maximumFractionDigits: 2 });
const number = new Intl.NumberFormat("en", { maximumFractionDigits: 3 });
const dateTime = new Intl.DateTimeFormat("en-GB", { dateStyle: "medium", timeStyle: "short" });
const byId = id => document.getElementById(id);

class ApiError extends Error {
    constructor(status, problem) {
        const titles = {
            400: "Please check your input",
            404: "This item is no longer available",
            409: "This room is already booked for that time"
        };
        super(problem?.title || titles[status] || "The request could not be completed");
        this.status = status;
        this.detail = problem?.detail || (status >= 500 ? "The server encountered a problem. Please try again." : "");
        this.validationMessages = Object.values(problem?.errors || {}).flat().filter(value => typeof value === "string");
    }
}

async function request(path, options = {}) {
    let response;
    try {
        response = await fetch(path, {
            ...options,
            headers: { Accept: "application/json", ...(options.body ? { "Content-Type": "application/json" } : {}) }
        });
    } catch {
        throw new ApiError(0, { title: "Unable to reach the API", detail: "Check your connection and try again." });
    }

    if (response.status === 204) return null;
    const contentType = response.headers.get("content-type") || "";
    const body = contentType.includes("json") ? await response.json() : null;
    if (!response.ok) throw new ApiError(response.status, body);
    return body;
}

function element(tag, text, className) {
    const node = document.createElement(tag);
    if (text !== undefined) node.textContent = String(text);
    if (className) node.className = className;
    return node;
}

function notify(title, detail = "", errors = [], isError = false) {
    const notification = byId("notification");
    notification.classList.toggle("error", isError);
    byId("notification-title").textContent = title;
    byId("notification-detail").textContent = detail;
    byId("notification-detail").hidden = !detail;
    byId("notification-errors").replaceChildren(...errors.map(message => element("li", message)));
    byId("notification-errors").hidden = errors.length === 0;
    notification.hidden = false;
    notification.scrollIntoView({ behavior: "smooth", block: "nearest" });
}

function showError(error) {
    if (error instanceof ApiError) {
        notify(error.message, error.detail, error.validationMessages, true);
        return;
    }
    notify("Something went wrong", "Please try again. If the problem persists, refresh the page.", [], true);
}

async function withLoading(button, label, action) {
    if (!button) {
        await action();
        return;
    }
    const originalText = button.textContent;
    button.disabled = true;
    button.textContent = label;
    try {
        await action();
    } finally {
        button.disabled = false;
        button.textContent = originalText;
    }
}

function activateTab(name, focus = false) {
    document.querySelectorAll("[data-tab]").forEach(tab => {
        const selected = tab.dataset.tab === name;
        tab.classList.toggle("active", selected);
        tab.setAttribute("aria-selected", String(selected));
        tab.tabIndex = selected ? 0 : -1;
        byId(`${tab.dataset.tab}-panel`).hidden = !selected;
        if (selected && focus) tab.focus();
    });
}

function serviceTags(services) {
    const container = element("div", undefined, "service-tags");
    if (!services.length) container.append(element("span", "No additional services", "muted"));
    services.forEach(service => container.append(element("span", service.name, "service-tag")));
    return container;
}

function serviceCheckboxes(container, services, selected = []) {
    container.replaceChildren();
    if (!services.length) {
        container.append(element("p", "No additional services available.", "muted"));
        return;
    }
    const selectedIds = new Set(selected);
    services.forEach(service => {
        const label = element("label", undefined, "service-option");
        const checkbox = document.createElement("input");
        checkbox.type = "checkbox";
        checkbox.name = "serviceIds";
        checkbox.value = service.id;
        checkbox.checked = selectedIds.has(service.id);
        label.append(checkbox, element("span", service.name), element("span", money.format(service.price), "service-price"));
        container.append(label);
    });
}

function selectedServices(container) {
    return [...container.querySelectorAll("input:checked")].map(input => Number(input.value));
}

function emptyTable(body, columns, message) {
    const row = document.createElement("tr");
    const cell = element("td", message, "empty-state");
    cell.colSpan = columns;
    row.append(cell);
    body.replaceChildren(row);
}

function actionButton(label, style, action, accessibleName) {
    const button = element("button", label, `button small ${style}`);
    button.type = "button";
    if (accessibleName) button.setAttribute("aria-label", accessibleName);
    button.addEventListener("click", action);
    return button;
}

function renderRooms() {
    const body = byId("rooms-body");
    body.replaceChildren();
    byId("room-count").textContent = state.rooms.length;
    byId("capacity-count").textContent = number.format(state.rooms.reduce((sum, room) => sum + room.capacity, 0));

    if (!state.rooms.length) emptyTable(body, 6, "No active rooms yet. Add your first room to get started.");

    state.rooms.forEach(room => {
        const row = document.createElement("tr");
        const services = document.createElement("td");
        services.append(serviceTags(room.services));
        const actions = document.createElement("td");
        const buttons = element("div", undefined, "row-actions");
        buttons.append(
            actionButton("Edit", "secondary", () => openRoomForm(room), `Edit ${room.name}`),
            actionButton("Delete", "danger", event => deleteRoom(room, event.currentTarget), `Delete ${room.name}`)
        );
        actions.append(buttons);
        row.append(element("td", `#${room.id}`, "id-cell"), element("td", room.name, "room-name"),
            element("td", `${number.format(room.capacity)} seats`, "number-cell"),
            element("td", money.format(room.baseHourlyRate), "number-cell"), services, actions);
        body.append(row);
    });
    renderBookingRooms();
}

function renderBookingRooms() {
    const select = byId("booking-room");
    const selectedId = Number(select.value);
    const checkedIds = selectedServices(byId("booking-services"));
    const placeholder = element("option", state.rooms.length ? "Select a room" : "No active rooms available");
    placeholder.value = "";
    select.replaceChildren(placeholder);
    state.rooms.forEach(room => {
        const option = element("option", `${room.name} · ${room.capacity} seats`);
        option.value = room.id;
        option.selected = room.id === selectedId;
        select.append(option);
    });
    renderBookingServices(checkedIds);
    byId("create-booking").disabled = !state.rooms.length;
}

function renderBookingServices(selected = []) {
    const room = state.rooms.find(item => item.id === Number(byId("booking-room").value));
    const container = byId("booking-services");
    if (!room) {
        container.replaceChildren(element("p", "Select a room to see its services.", "muted"));
        return;
    }
    serviceCheckboxes(container, room.services, selected);
}

function resetAvailability() {
    byId("availability-results").replaceChildren();
    byId("availability-summary").textContent = "Choose a time to see matching rooms.";
}

async function loadRooms() {
    state.rooms = await request("/api/rooms");
    renderRooms();
}

async function loadWorkspace() {
    try {
        const [rooms, services] = await Promise.all([request("/api/rooms"), request("/api/services")]);
        state.rooms = rooms;
        state.services = services;
        renderRooms();
        byId("add-room").disabled = false;
        byId("connection-status").textContent = "● API connected";
        byId("connection-status").className = "connection-status connected";
    } catch (error) {
        byId("connection-status").textContent = "API unavailable";
        byId("connection-status").className = "connection-status offline";
        if (!state.rooms.length) emptyTable(byId("rooms-body"), 6, "Rooms could not be loaded. Use Refresh to try again.");
        showError(error);
    }
}

function openRoomForm(room = null) {
    state.editingRoomId = room?.id ?? null;
    byId("room-form").reset();
    byId("room-dialog-title").textContent = room ? "Edit room" : "Add room";
    byId("save-room").textContent = room ? "Save changes" : "Create room";
    byId("room-name").value = room?.name ?? "";
    byId("room-capacity").value = room?.capacity ?? "";
    byId("room-rate").value = room?.baseHourlyRate ?? "";
    byId("room-form-error").hidden = true;
    serviceCheckboxes(byId("room-services"), state.services, room?.services.map(service => service.id) ?? []);
    byId("room-dialog").showModal();
    byId("room-name").focus();
}

async function saveRoom(event) {
    event.preventDefault();
    const editingId = state.editingRoomId;
    const payload = {
        name: byId("room-name").value,
        capacity: Number(byId("room-capacity").value),
        baseHourlyRate: Number(byId("room-rate").value),
        serviceIds: selectedServices(byId("room-services"))
    };
    byId("room-form-error").hidden = true;
    await withLoading(byId("save-room"), "Saving…", async () => {
        try {
            const room = await request(editingId ? `/api/rooms/${editingId}` : "/api/rooms", {
                method: editingId ? "PUT" : "POST", body: JSON.stringify(payload)
            });
            byId("room-dialog").close();
            notify(editingId ? "Room updated" : "Room created", `${room.name} is ready to book.`);
            resetAvailability();
            await loadRooms();
        } catch (error) {
            if (byId("room-dialog").open) {
                const messages = error instanceof ApiError
                    ? [error.message, error.detail, ...error.validationMessages].filter(Boolean)
                    : ["The room could not be saved. Please try again."];
                byId("room-form-error").textContent = messages.join("\n");
                byId("room-form-error").hidden = false;
            } else {
                showError(error);
            }
        }
    });
}

async function deleteRoom(room, button) {
    if (!window.confirm(`Delete “${room.name}”? Existing bookings will be preserved, and this room will no longer be available.`)) return;
    await withLoading(button, "Deleting…", async () => {
        try {
            await request(`/api/rooms/${room.id}`, { method: "DELETE" });
            notify("Room deleted", `${room.name} has been removed from the active room directory.`);
            resetAvailability();
            await loadRooms();
        } catch (error) {
            showError(error);
        }
    });
}

async function findRooms(event) {
    event.preventDefault();
    const start = byId("availability-start").value;
    const end = byId("availability-end").value;
    const query = new URLSearchParams({ startDateTime: start, endDateTime: end, capacity: byId("availability-capacity").value });
    await withLoading(event.submitter, "Finding rooms…", async () => {
        try {
            const rooms = await request(`/api/rooms/available?${query}`);
            const results = byId("availability-results");
            results.replaceChildren();
            byId("availability-summary").textContent = `${rooms.length} ${rooms.length === 1 ? "room" : "rooms"} available for your meeting.`;
            if (!rooms.length) results.append(element("p", "No rooms available for the selected period.", "empty-state"));
            rooms.forEach(room => {
                const card = element("article", undefined, "card available-room");
                const heading = document.createElement("div");
                heading.append(element("h3", room.name), element("p", `${room.capacity} seats · Room #${room.id}`, "muted"));
                const rate = element("p", money.format(room.baseHourlyRate), "room-rate");
                rate.append(element("span", "base rate per hour"));
                const book = actionButton("Book this room", "primary", () => {
                    byId("booking-room").value = room.id;
                    byId("booking-start").value = start;
                    byId("booking-end").value = end;
                    renderBookingServices();
                    activateTab("booking", true);
                });
                card.append(heading, rate, serviceTags(room.services), book);
                results.append(card);
            });
            byId("notification").hidden = true;
        } catch (error) {
            resetAvailability();
            showError(error);
        }
    });
}

function displayDateTime(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : dateTime.format(date);
}

function renderReceipt(booking) {
    const receipt = byId("booking-receipt");
    const details = element("dl", undefined, "receipt-details");
    const entries = [
        ["Booking ID", `#${booking.bookingId}`],
        ["Room", booking.room.name],
        ["Start", displayDateTime(booking.startDateTime)],
        ["End", displayDateTime(booking.endDateTime)],
        ["Selected services", booking.selectedServices.map(service => service.name).join(", ") || "None"],
        ["Rental price", money.format(booking.rentalPrice)],
        ["Services price", money.format(booking.servicesPrice)]
    ];
    entries.forEach(([label, value]) => details.append(element("dt", label), element("dd", value)));
    const total = element("div", undefined, "receipt-total");
    total.append(element("span", "Total price"), element("strong", money.format(booking.totalPrice)));
    receipt.replaceChildren(element("span", "✓", "receipt-symbol"), element("h3", "Booking confirmed"), details, total);
}

async function createBooking(event) {
    event.preventDefault();
    const payload = {
        roomId: Number(byId("booking-room").value),
        startDateTime: byId("booking-start").value,
        endDateTime: byId("booking-end").value,
        serviceIds: selectedServices(byId("booking-services"))
    };
    await withLoading(event.submitter, "Creating booking…", async () => {
        try {
            const booking = await request("/api/bookings", { method: "POST", body: JSON.stringify(payload) });
            renderReceipt(booking);
            resetAvailability();
            byId("reports-results").hidden = true;
            notify("Booking confirmed", `Your reservation for ${booking.room.name} has been saved. Booking #${booking.bookingId}.`);
        } catch (error) {
            showError(error);
        }
    });
}

function renderReportTable(body, rows, columns) {
    body.replaceChildren();
    if (!rows.length) {
        emptyTable(body, columns.length, "No data for the selected period.");
        return;
    }
    rows.forEach(item => {
        const row = document.createElement("tr");
        columns.forEach((column, index) => row.append(element("td", column(item), index === 0 ? "room-name" : "number-cell")));
        body.append(row);
    });
}

async function loadReports(event) {
    event.preventDefault();
    const query = new URLSearchParams({ from: byId("reports-from").value, to: byId("reports-to").value });
    await withLoading(event.submitter, "Loading reports…", async () => {
        try {
            const [revenue, rooms, services] = await Promise.all([
                request(`/api/reports/revenue?${query}`), request(`/api/reports/rooms?${query}`), request(`/api/reports/services?${query}`)
            ]);
            byId("report-revenue").textContent = money.format(revenue.totalRevenue);
            byId("report-booking-count").textContent = number.format(revenue.bookingCount);
            byId("report-average").textContent = money.format(revenue.averageBookingValue);
            renderReportTable(byId("report-rooms-body"), rooms, [
                room => `${room.roomName} (#${room.roomId})`, room => number.format(room.bookingCount),
                room => number.format(room.bookedHours), room => money.format(room.revenue)
            ]);
            renderReportTable(byId("report-services-body"), services, [
                service => `${service.serviceName} (#${service.serviceId})`,
                service => number.format(service.timesSelected), service => money.format(service.revenue)
            ]);
            byId("reports-results").hidden = false;
            byId("notification").hidden = true;
        } catch (error) {
            byId("reports-results").hidden = true;
            showError(error);
        }
    });
}

function localInputDate(date) {
    const pad = value => String(value).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function setDefaultDates() {
    const start = new Date();
    start.setDate(start.getDate() + 1);
    start.setHours(10, 0, 0, 0);
    const end = new Date(start);
    end.setHours(12);
    ["availability-start", "booking-start"].forEach(id => { byId(id).value = localInputDate(start); });
    ["availability-end", "booking-end"].forEach(id => { byId(id).value = localInputDate(end); });
    byId("reports-from").value = localInputDate(new Date(start.getFullYear(), start.getMonth(), 1));
    byId("reports-to").value = localInputDate(new Date(start.getFullYear(), start.getMonth() + 1, 1));
}

document.querySelectorAll("[data-tab]").forEach(tab => tab.addEventListener("click", () => activateTab(tab.dataset.tab)));
document.querySelector(".tabs").addEventListener("keydown", event => {
    const tabs = [...document.querySelectorAll("[data-tab]")];
    const current = tabs.indexOf(document.activeElement);
    if (current < 0) return;
    let next;
    if (event.key === "ArrowDown" || event.key === "ArrowRight") next = (current + 1) % tabs.length;
    if (event.key === "ArrowUp" || event.key === "ArrowLeft") next = (current + tabs.length - 1) % tabs.length;
    if (event.key === "Home") next = 0;
    if (event.key === "End") next = tabs.length - 1;
    if (next !== undefined) {
        event.preventDefault();
        activateTab(tabs[next].dataset.tab, true);
    }
});
byId("notification-close").addEventListener("click", () => { byId("notification").hidden = true; });
byId("add-room").addEventListener("click", () => openRoomForm());
byId("refresh-rooms").addEventListener("click", event => withLoading(event.currentTarget, "Refreshing…", loadWorkspace));
byId("close-room-dialog").addEventListener("click", () => byId("room-dialog").close());
byId("cancel-room-dialog").addEventListener("click", () => byId("room-dialog").close());
byId("room-form").addEventListener("submit", saveRoom);
byId("availability-form").addEventListener("submit", findRooms);
byId("booking-room").addEventListener("change", () => renderBookingServices());
byId("booking-form").addEventListener("submit", createBooking);
byId("reports-form").addEventListener("submit", loadReports);

setDefaultDates();
loadWorkspace();
