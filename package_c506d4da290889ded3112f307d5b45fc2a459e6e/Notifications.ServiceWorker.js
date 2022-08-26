/* eslint no-console: ["error", { allow: ["warn", "error"] }] */
//@ts-ignore
_self = self;
var Notifications;
(function (Notifications) {
    var ServiceWorker;
    (function (ServiceWorker) {
        class NotificationWorker {
            static Run() {
                if (self.document) {
                    return;
                }
                self.onmessage = this.HandleMessage.bind(this);
                self.addEventListener("install", function (event) {
                    event.waitUntil(_self.skipWaiting()); // Activate worker immediately
                });
                self.addEventListener("activate", function (event) {
                    event.waitUntil(_self.clients.claim()); // Become available to all pages
                });
                // @ts-ignore
                self.onnotificationclick = (event) => {
                    event.notification.close();
                    var eventId;
                    if (event.action) {
                        eventId = event.action;
                    }
                    else {
                        eventId = event.notification.tag;
                    }
                    var eventData = JSON.parse(eventId);
                    var type = eventData.type;
                    var guid = eventData.guid;
                    switch (type) {
                        case "dismiss":
                            break;
                        case "background":
                            throw new DOMException("Uno Platform does not support background tasks", "System.NotImplementedException");
                        case "protocol":
                            var url = eventData.argument;
                            _self.clients.openWindow(url);
                            break;
                        case "foreground":
                            var mainPageUrl = (new URL("..", _self.registration.scope)).href;
                            // This looks to see if the current is already open and
                            // focuses if it is
                            event.waitUntil(new Promise(resolve => {
                                _self.clients.matchAll({
                                    includeUncontrolled: true,
                                    type: "window"
                                }).then((clientWindows) => {
                                    clientWindows.reduce((p, client) => {
                                        return p.then((isSuccessful) => {
                                            if (isSuccessful) {
                                                return Promise.resolve(true);
                                            }
                                            return new Promise(resolveChild => {
                                                if (client.url !== mainPageUrl) {
                                                    resolveChild(false);
                                                }
                                                if (!("focus" in client)) {
                                                    resolveChild(false);
                                                }
                                                new Promise(resolveCommunication => {
                                                    var tempChannel = new MessageChannel();
                                                    tempChannel.port1.onmessage = (ev) => {
                                                        resolveCommunication(ev.data.payload);
                                                    };
                                                    client.postMessage({ op: "CHECK_GUID", payload: guid }, [tempChannel.port2]);
                                                    setTimeout(() => resolveCommunication(false), 1000);
                                                }).then((isSuccessful) => {
                                                    if (!isSuccessful) {
                                                        resolveChild(false);
                                                    }
                                                    else {
                                                        client.focus().then((_) => {
                                                            this.PumpMessage(client, "notificationclick", eventData.argument);
                                                            resolveChild(true);
                                                        });
                                                    }
                                                });
                                            });
                                        });
                                    }, Promise.resolve(false)).then((isSuccessful) => {
                                        if (isSuccessful) {
                                            resolve();
                                        }
                                        else if (_self.clients.openWindow) {
                                            _self.clients.openWindow(mainPageUrl).then((client) => {
                                                this.PumpMessage(client, "notificationclick", eventData.argument);
                                            });
                                        }
                                    });
                                });
                            }));
                            break;
                    }
                };
            }
            static async PumpMessage(client, eventOp, eventArg) {
                var num = this._num++;
                for (var i = 0; i < 128; ++i) {
                    var doWork = new Promise(resolve => {
                        this._resolves[num] = resolve;
                        var tempChannel = new MessageChannel();
                        tempChannel.port1.onmessage = this.HandleMessage.bind(this);
                        client.postMessage({ op: eventOp, payload: { arg: eventArg, seq: num } }, [tempChannel.port2]);
                    });
                    var delay = new Promise(resolve => {
                        this.Delay(this._timeout).then(() => resolve(false));
                    });
                    if (await Promise.race([doWork, delay])) {
                        break;
                    }
                    console.warn("Failed to send notification. Retrying... " + i);
                }
                delete this._resolves[num];
            }
            static Delay(milliseconds) {
                return new Promise(resolve => {
                    setTimeout(() => resolve(), milliseconds);
                });
            }
            static HandleMessage(event) {
                if (event.data.op) {
                    switch (event.data.op) {
                        case "SET_PORT":
                            var guid = event.data.payload.guid;
                            console.warn("WTF why are you sending ports?");
                            //this._ports[guid] = event.ports[0];
                            //this._ports[guid].onmessage = this.HandleMessage.bind(this);
                            //this._ports[guid].postMessage({ op: "ack" });
                            break;
                        case "REMOVE_PORT":
                            //var guid = event.data.payload.guid;
                            //delete this._ports[guid];
                            //break;
                            console.warn("Do I have any ports to remove?");
                            break;
                        case "SHOW_NOTIFICATION":
                            // @ts-ignore
                            self.registration.showNotification(event.data.payload.title, event.data.payload.options);
                            break;
                        case "ACK":
                            var num = event.data.payload;
                            if (num in this._resolves) {
                                this._resolves[num](true);
                            }
                            break;
                    }
                }
            }
        }
        NotificationWorker._resolves = {};
        NotificationWorker._num = 0;
        // This should be adequate for most responsive modern browsers.
        NotificationWorker._timeout = 2048;
        ServiceWorker.NotificationWorker = NotificationWorker;
    })(ServiceWorker = Notifications.ServiceWorker || (Notifications.ServiceWorker = {}));
})(Notifications || (Notifications = {}));
Notifications.ServiceWorker.NotificationWorker.Run();
