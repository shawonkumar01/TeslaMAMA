// SignalR connection management
var signalRConnection = null;

function initializeSignalR(userId, role) {
    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl("/ridehub")
        .withAutomaticReconnect()
        .build();

    signalRConnection.start()
        .then(function () {
            console.log("SignalR Connected");

            // Join appropriate group based on role
            if (role === "Passenger") {
                signalRConnection.invoke("JoinPassengerGroup", userId);
            } else if (role === "Driver") {
                signalRConnection.invoke("JoinDriverGroup", userId);
            }
        })
        .catch(function (err) {
            console.error("SignalR Connection Error: ", err);
        });

    // Handle connection close
    signalRConnection.onclose(function () {
        console.log("SignalR Connection Closed");
    });

    // Handle reconnection
    signalRConnection.onreconnecting(function () {
        console.log("SignalR Reconnecting...");
    });

    signalRConnection.onreconnected(function () {
        console.log("SignalR Reconnected");
        // Re-join group after reconnection
        if (role === "Passenger") {
            signalRConnection.invoke("JoinPassengerGroup", userId);
        } else if (role === "Driver") {
            signalRConnection.invoke("JoinDriverGroup", userId);
        }
    });
}

// Passenger event handlers
function onRideCreated(callback) {
    if (signalRConnection) {
        signalRConnection.on("RideCreated", callback);
    }
}

function onRideStatusChanged(callback) {
    if (signalRConnection) {
        signalRConnection.on("RideStatusChanged", callback);
    }
}

// Driver event handlers
function onNewOffer(callback) {
    if (signalRConnection) {
        signalRConnection.on("NewOffer", callback);
    }
}

// Clean up when leaving page
function cleanupSignalR(userId, role) {
    if (signalRConnection) {
        if (role === "Passenger") {
            signalRConnection.invoke("LeavePassengerGroup", userId);
        } else if (role === "Driver") {
            signalRConnection.invoke("LeaveDriverGroup", userId);
        }
        signalRConnection.stop();
    }
}
