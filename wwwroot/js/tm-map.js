window.TeslaMAMA = window.TeslaMAMA || {};

TeslaMAMA.Map = (function () {
    const DHAKA_CENTER = [23.7925, 90.4078];
    const GREEN = "#1a6e55";
    const groups = new WeakMap();      // one layer group per map
    const tokens = new WeakMap();      // detects outdated responses
    const cache  = new Map();          // avoids repeat requests for the same route
    const resizeObservers = new WeakMap();  // ResizeObserver for each map

    function init(elementId, options) {
        const opts = Object.assign({ center: DHAKA_CENTER, zoom: 11, interactive: true, labels: true }, options);
        const element = document.getElementById(elementId);

        if (!element) {
            console.error(`Map element with id "${elementId}" not found`);
            return null;
        }

        const mapOptions = {
            center: opts.center,
            zoom: opts.zoom,
            minZoom: 8,
            scrollWheelZoom: false,
            dragging: opts.interactive ? !L.Browser.mobile : false,
            touchZoom: opts.interactive,
            doubleClickZoom: opts.interactive,
            boxZoom: opts.interactive,
            keyboard: opts.interactive,
            zoomControl: opts.interactive,
            attributionControl: false
        };

        const map = L.map(elementId, mapOptions);

        L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(map);

        L.control.attribution({ prefix: false }).addTo(map);

        if (opts.interactive) {
            map.on("click", () => map.scrollWheelZoom.enable());
            map.on("mouseout", () => map.scrollWheelZoom.disable());
        }

        groups.set(map, L.layerGroup().addTo(map));
        tokens.set(map, 0);

        // Add ResizeObserver to handle container size changes
        const resizeObserver = new ResizeObserver(() => {
            map.invalidateSize();
        });
        resizeObserver.observe(element);
        resizeObservers.set(map, resizeObserver);

        return map;
    }

    function pin(point, fill, stroke, radius = 9, permanent = true) {
        const marker = L.circleMarker([point.lat, point.lng], {
            radius: radius,
            weight: 3,
            color: stroke,
            fillColor: fill,
            fillOpacity: 1
        });

        if (permanent) {
            marker.bindTooltip(point.name, {
                permanent: true,
                direction: "top",
                offset: [0, -10],
                className: "tm-map-label"
            });
        }

        return marker;
    }

    // OSRM expects longitude,latitude order (the opposite of Leaflet)
    async function fetchRoadRoute(from, to) {
        const key = [from.lat, from.lng, to.lat, to.lng].join(",");
        if (cache.has(key)) return cache.get(key);

        const url = "https://router.project-osrm.org/route/v1/driving/" +
            from.lng + "," + from.lat + ";" + to.lng + "," + to.lat +
            "?overview=full&geometries=geojson";

        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(), 8000);   // give up after 8 seconds

        try {
            const response = await fetch(url, { signal: controller.signal });
            if (!response.ok) throw new Error("OSRM responded " + response.status);

            const data = await response.json();
            if (data.code !== "Ok" || !data.routes.length) throw new Error("No route found");

            const route = data.routes[0];
            const result = {
                latlngs: route.geometry.coordinates.map(c => [c[1], c[0]]),  // swap to lat,lng
                distanceKm: route.distance / 1000,                           // metres to km
                durationMin: route.duration / 60                             // seconds to minutes
            };
            cache.set(key, result);
            return result;
        } finally {
            clearTimeout(timer);
        }
    }

    async function setRoute(map, from, to, options = {}) {
        const opts = Object.assign({ labels: true, padding: [24, 24] }, options);
        const token = (tokens.get(map) || 0) + 1;
        tokens.set(map, token);

        const group = groups.get(map);
        group.clearLayers();

        const markerRadius = opts.labels ? 9 : 7;
        pin(from, "#a9e5cb", GREEN, markerRadius, opts.labels).addTo(group);
        pin(to, "#ff6b6b", "#c92a2a", markerRadius, opts.labels).addTo(group);

        // Invalidate size to ensure correct bounds calculation
        map.invalidateSize();

        // Check if pickup and destination are the same coordinates
        const isSameCoords = from.lat === to.lat && from.lng === to.lng;

        if (isSameCoords) {
            map.setView([from.lat, from.lng], 14);
        } else {
            map.fitBounds([[from.lat, from.lng], [to.lat, to.lng]], { padding: opts.padding, maxZoom: 15 });
        }

        let road = null;
        try {
            road = await fetchRoadRoute(from, to);
        } catch (error) {
            console.warn("Road routing unavailable, using straight line:", error);
        }

        if (tokens.get(map) !== token) return null;     // user changed zones meanwhile

        if (road) {
            // white casing under the green line makes it readable on busy road maps
            L.polyline(road.latlngs, { color: "#ffffff", weight: 9, opacity: 0.9 }).addTo(group);
            const line = L.polyline(road.latlngs, { color: GREEN, weight: 5, opacity: 0.95 }).addTo(group);
            map.fitBounds(line.getBounds(), { padding: opts.padding, maxZoom: 15 });
            return { isRoad: true, distanceKm: road.distanceKm, durationMin: road.durationMin };
        }

        if (!isSameCoords) {
            L.polyline([[from.lat, from.lng], [to.lat, to.lng]], {
                color: GREEN, weight: 4, opacity: 0.85, dashArray: "8 10"
            }).addTo(group);
        }
        return { isRoad: false };
    }

    function clearRoute(map) {
        tokens.set(map, (tokens.get(map) || 0) + 1);    // cancels any pending response
        groups.get(map).clearLayers();
        map.setView(DHAKA_CENTER, 11);
    }

    function destroy(map) {
        if (!map) return;

        // Clean up ResizeObserver
        const resizeObserver = resizeObservers.get(map);
        if (resizeObserver) {
            resizeObserver.disconnect();
            resizeObservers.delete(map);
        }

        // Clean up map
        map.remove();
    }

    return { init, setRoute, clearRoute, destroy };
})();
