function initMap() {
    if (window.MapView && typeof window.MapView.initialize === 'function') {
        window.MapView.initialize();
    } else {
        console.error("MapView module is not ready or defined.");
    }
}

(function (window, document) {
    let googleMap = null;
    let appointmentMarkers = [];
    let infoWindow = null;
    const geocodeCache = new Map();
    let isInitialized = false;

   
    function loadGoogleMapsScript() {
        const mapContainer = document.querySelector('[data-google-maps-api-key]');
        const apiKey = mapContainer ? mapContainer.dataset.googleMapsApiKey : null;

        if (!apiKey) {
            console.error("Google Maps API Key not found.");
            const mapDiv = document.getElementById('mapViewContainer');
            if (mapDiv) {
                mapDiv.innerHTML = '<div class="map-error"><strong>Configuration Error:</strong> Google Maps API key is missing.</div>';
            }
            return;
        }

        if (window.google && window.google.maps) {
            initMap();
            return;
        }

        const script = document.createElement('script');
        script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=places,marker&callback=initMap`;
        script.async = true;
        script.defer = true;
        document.head.appendChild(script);
    }


    function showMapLoading(show) {
        const mapContainer = document.getElementById('mapViewContainer');
        if (!mapContainer) return;

        let loadingOverlay = mapContainer.querySelector('.map-loading-overlay');
        if (show) {
            if (!loadingOverlay) {
                loadingOverlay = document.createElement('div');
                loadingOverlay.className = 'map-loading-overlay';
                mapContainer.appendChild(loadingOverlay);
            }
            loadingOverlay.style.display = 'flex';
        } else {
            if (loadingOverlay) {
                loadingOverlay.style.display = 'none';
            }
        }
    }

   
    function showNoAppointmentsMessage(show, text = 'No appointments found for the selected criteria.') {
        const mapContainer = document.getElementById('mapViewContainer');
        if (!mapContainer) return;

        let messageOverlay = mapContainer.querySelector('.map-message-overlay');
        if (show) {
            if (!messageOverlay) {
                messageOverlay = document.createElement('div');
                messageOverlay.className = 'map-message-overlay';
                messageOverlay.innerHTML = `
                    <div class="message-content">
                        <div class="icon">🗺️</div>
                        <h5>No Appointments Found</h5>
                        <p>${text}</p>
                    </div>`;
                mapContainer.appendChild(messageOverlay);
            }
            messageOverlay.style.display = 'flex';
        } else {
            if (messageOverlay) {
                messageOverlay.style.display = 'none';
            }
        }
    }

    async function getCoordinates(address) {
        if (!address || address.trim().length < 5) return null;
        const cleanAddress = address.trim();
        if (geocodeCache.has(cleanAddress)) return geocodeCache.get(cleanAddress);

        try {
            const geocoder = new google.maps.Geocoder();
            const response = await geocoder.geocode({ address: cleanAddress });
            if (response.results && response.results.length > 0) {
                const location = response.results[0].geometry.location;
                const coords = { lat: location.lat(), lng: location.lng() };
                geocodeCache.set(cleanAddress, coords);
                return coords;
            }
        } catch (error) {
            console.warn(`Geocoding failed for "${cleanAddress}":`, error.message);
        }
        geocodeCache.set(cleanAddress, null);
        return null;
    }

    function createMapMarker(appt, coords, address) {
        const marker = new google.maps.marker.AdvancedMarkerElement({
            position: coords,
            map: googleMap,
            title: `${appt?.CustomerName || 'Unknown'} - ${appt?.ServiceType || 'No Service'}`,
        });

        marker.addListener('click', () => {
            if (infoWindow) infoWindow.close();

            const fullAddress = [appt.SiteAddress || appt.Address1, appt.City, appt.State, appt.ZipCode].filter(Boolean).join(', ') || 'N/A';

            const content = `
            <div class="map-infowindow">
                <h4 class="infowindow-customer">${appt.CustomerName || 'N/A'}</h4>
                <div class="infowindow-section">
                    <div class="infowindow-row"><span class="infowindow-label">Service:</span> <span class="infowindow-value">${appt.ServiceType || 'N/A'}</span></div>
                    <div class="infowindow-row"><span class="infowindow-label">Status:</span> <span class="infowindow-value status">${appt.AppoinmentStatus || 'N/A'}</span></div>
                    <div class="infowindow-row"><span class="infowindow-label">Date:</span> <span class="infowindow-value">${formatToUSDate(appt.RequestDate) || 'N/A'}</span></div>
                    <div class="infowindow-row"><span class="infowindow-label">Time Slot:</span> <span class="infowindow-value">${formatTimeRange(appt.TimeSlot) || 'N/A'}</span></div>
                    <div class="infowindow-row"><span class="infowindow-label">Duration:</span> <span class="infowindow-value">${appt.Duration || 'N/A'}</span></div>
                    <div class="infowindow-row"><span class="infowindow-label">Resource:</span> <span class="infowindow-value">${appt.ResourceName || 'Unassigned'}</span></div>
                    <div class="infowindow-row"><span class="infowindow-label">Address:</span> <span class="infowindow-value">${fullAddress}</span></div>
                </div>
                <button onclick="if(typeof openEditModal === 'function') { openEditModal('${appt.AppoinmentId}'); } else { console.error('openEditModal function not found'); }" class="btn btn-primary btn-sm mt-2">View Details</button>
            </div>
        `;

            infoWindow = new google.maps.InfoWindow({
                content,
                maxWidth: 350
            });

            infoWindow.open(googleMap, marker);
        });

        return marker;
    }


 
    function filterAppointments(appointments) {
        const statusFilter = $('#MainContent_statusFilter').val() || 'all';
        const serviceTypeFilter = $('#MainContent_mapServiceTypeFilter').val() || 'all';

        return appointments.filter(appt => {
            const hasAddress = !!(appt?.SiteAddress || appt?.Address1);
            if (!hasAddress) return false;

            // Match by ID if available, otherwise by name
            let matchesStatus = statusFilter === 'all';
            if (!matchesStatus) {
                const statusId = appt?.AppoinmentStatusID || appt?.StatusID;
                if (statusId && /^\d+$/.test(statusFilter)) {
                    matchesStatus = String(statusId) === String(statusFilter);
                } else {
                    matchesStatus = (appt?.AppoinmentStatus || '').toLowerCase() === statusFilter.toLowerCase();
                }
            }
            
            let matchesService = serviceTypeFilter === 'all';
            if (!matchesService) {
                const serviceId = appt?.ServiceTypeID || appt?.ServiceTypeId;
                if (serviceId && /^\d+$/.test(serviceTypeFilter)) {
                    matchesService = String(serviceId) === String(serviceTypeFilter);
                } else {
                    matchesService = (appt?.ServiceType || '').toLowerCase() === serviceTypeFilter.toLowerCase();
                }
            }

            return matchesStatus && matchesService;
        });
    }

 
    async function renderAllAppointmentMarkers() {
        if (!googleMap) return;


        showNoAppointmentsMessage(false);
        appointmentMarkers.forEach(marker => marker.map = null);
        appointmentMarkers = [];

        if (!window.appointments || window.appointments.length === 0) {
            showNoAppointmentsMessage(true, 'No appointments found for the selected date.');
            showMapLoading(false);
            return;
        }

        const filteredAppointments = filterAppointments(window.appointments);
        if (filteredAppointments.length === 0) {
            showNoAppointmentsMessage(true, 'No appointments match the current filter criteria.');
            showMapLoading(false);
            return;
        }

        const bounds = new google.maps.LatLngBounds();
        let markersCreated = 0;

        for (const appt of filteredAppointments) {
            const address = appt?.SiteAddress || appt?.Address1;
            const coords = await getCoordinates(address);
            if (coords) {
                const marker = createMapMarker(appt, coords, address);
                appointmentMarkers.push(marker);
                bounds.extend(coords);
                markersCreated++;
            }
        }

        if (markersCreated > 0) {
            googleMap.fitBounds(bounds);
        } else {
            showNoAppointmentsMessage(true, 'No appointments with valid addresses could be located.');
        }

        showMapLoading(false);
    }

 
    function loadAppointmentsForMap() {
        if (!googleMap) return;
        showMapLoading(true);

        const fromDate = $('#dayDatePicker').val() || new Date().toISOString().split('T')[0];

        getAppoinments('', fromDate, fromDate, fromDate, function (appointments) {
            window.appointments = appointments;
            renderAllAppointmentMarkers();
        });
    }

    function initializeMapView() {
        if (isInitialized) return;

        const mapContainer = document.getElementById('mapViewContainer');
        if (!mapContainer) return;


        const mapCanvas = document.createElement('div');
        mapCanvas.className = 'map-canvas';
        mapContainer.appendChild(mapCanvas);

        googleMap = new google.maps.Map(mapCanvas, {
            center: { lat: 40.7128, lng: -74.0060 },
            zoom: 4,
            mapId: 'FSM_MAP_ID',
            mapTypeControl: false, 
            streetViewControl: true,
            fullscreenControl: true,
            gestureHandling: 'greedy' // Isolate map scroll from page scroll
        });
        
        // Prevent map scroll from affecting other tabs
        google.maps.event.addListener(googleMap, 'dragstart', function() {
            document.body.style.overflow = 'hidden';
        });
        google.maps.event.addListener(googleMap, 'dragend', function() {
            document.body.style.overflow = '';
        });

        const controlDiv = document.createElement('div');
        controlDiv.style.margin = '10px';
        controlDiv.style.display = 'flex';
        controlDiv.style.boxShadow = '0 2px 6px rgba(0,0,0,.3)';
        controlDiv.style.borderRadius = '5px';
        controlDiv.style.overflow = 'hidden';

        const mapTypeButton = document.createElement('button');
        mapTypeButton.type = 'button';
        mapTypeButton.className = 'btn btn-light active'; 
        mapTypeButton.textContent = 'Map';
        controlDiv.appendChild(mapTypeButton);

        const satelliteTypeButton = document.createElement('button');
        satelliteTypeButton.type = 'button';
        satelliteTypeButton.className = 'btn btn-light';
        satelliteTypeButton.textContent = 'Satellite';
        controlDiv.appendChild(satelliteTypeButton);


        mapTypeButton.addEventListener('click', () => {
            googleMap.setMapTypeId('roadmap');
            mapTypeButton.classList.add('active');
            satelliteTypeButton.classList.remove('active');
        });

        satelliteTypeButton.addEventListener('click', () => {
            googleMap.setMapTypeId('satellite');
            satelliteTypeButton.classList.add('active');
            mapTypeButton.classList.remove('active');
        });


        googleMap.controls[google.maps.ControlPosition.TOP_RIGHT].push(controlDiv);
     

        isInitialized = true;
        attachEventListeners();
        loadAppointmentsForMap();
    }

    function attachEventListeners() {
        $("#mapReloadBtn").on('click', function (e) {
            e.preventDefault();
            loadAppointmentsForMap();
        });

        $("#MainContent_statusFilter, #MainContent_mapServiceTypeFilter").on('change', loadAppointmentsForMap);

        $('a[data-bs-toggle="tab"][href="#mapView"]').on('shown.bs.tab', function () {
            setTimeout(() => {
                if (isInitialized) {
                    google.maps.event.trigger(googleMap, 'resize');
                    loadAppointmentsForMap(); 
                }
            }, 100);
        });
    }

    window.MapView = {
        initialize: initializeMapView,
        reload: loadAppointmentsForMap
    };

    document.addEventListener('DOMContentLoaded', loadGoogleMapsScript);

})(window, document);