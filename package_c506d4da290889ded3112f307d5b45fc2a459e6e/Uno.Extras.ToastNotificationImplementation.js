var Uno;
(function (Uno) {
    var Extras;
    (function (Extras) {
        class ToastNotificationImplementation {
            static GetScriptsLocation() {
                var scriptLocation = "";
                var unoAppBase = config.uno_app_base;
                // Using some Uno Platform internal variables.
                if (typeof (unoAppBase) == "string") {
                    scriptLocation += "./" + unoAppBase;
                }
                // A fallback using the Document.
                else {
                    scriptLocation += Array
                        .from(document.scripts)
                        .map(script => script.baseURI + script.attributes.getNamedItem("src").value)
                        .find(name => name.endsWith("dotnet.js"))
                        .match(/([\S\s]*package_[a-zA-Z0-9]*)/g)[0];
                }
                scriptLocation += "/";
                return scriptLocation;
            }
            static LoadIFrameAsync(src) {
                return new Promise(resolve => {
                    var frame = document.createElement("iframe");
                    frame.height = "0";
                    frame.width = "0";
                    frame.src = src;
                    frame.addEventListener("load", ev => { resolve(frame); });
                    document.body.appendChild(frame);
                });
            }
            static async InitAsync() {
                // We must initialize an iframe, so that the Service Worker's scope could cover 
                // the notifications.
                var location = this.GetScriptsLocation();
                this._frame = await this.LoadIFrameAsync(location);
                this._serviceWorker = await this._frame.contentWindow.navigator.serviceWorker.register(this.ServiceWorkerName);
                await this._frame.contentWindow.navigator.serviceWorker.ready;
                while (!this._frame.contentWindow.navigator.serviceWorker.controller) {
                    await new Promise(f => setTimeout(f, 100));
                }
                this._guid = this.GenerateGuid();
            }
            static GenerateGuid() {
                const a = crypto.getRandomValues(new Uint16Array(8));
                let i = 0;
                return "00-0-4-1-000".replace(/[^-]/g, s => (a[i++] + Number.parseInt(s) * 0x10000 >> Number.parseInt(s)).toString(16).padStart(4, "0"));
            }
            static HandleMessage(ev) {
                if (ev.data.op) {
                    switch (ev.data.op) {
                        case "notificationclick":
                            this._callbackFunc(ev.data.payload.toString());
                            break;
                    }
                }
            }
            static SetServiceWorkerName(name) {
                this.ServiceWorkerName = name;
                this._doneInit = false;
            }
            static SetNotificationClickHandler(managedName) {
                this._callbackFunc = Module.mono_bind_static_method(managedName);
            }
            static async Show(title, options, actions) {
                if (!this._doneInit) {
                    await this.InitAsync();
                    this._doneInit = true;
                }
                for (var action of actions) {
                    action.guid = this._guid;
                }
                var actionsArr = [];
                for (var i = 1; i < actions.length; ++i) {
                    actionsArr.push({ title: actions[i].title, action: JSON.stringify(actions[i]) });
                }
                options.actions = actionsArr;
                options.tag = JSON.stringify(actions[0]);
                this._serviceWorker.showNotification(title, options);
                //this._frame.contentWindow.navigator.serviceWorker.controller.postMessage({ op: "SHOW_NOTIFICATION", payload: { title: title, options: options } });
            }
            static QueryPermissionAsync() {
                return new Promise(resolve => {
                    function handlePermission(permission) {
                        resolve(permission.toString());
                    }
                    // Check Notification.requestPermission's syntax
                    function checkNotificationPromise() {
                        try {
                            Notification.requestPermission().then();
                        }
                        catch (e) {
                            return false;
                        }
                        return true;
                    }
                    // Let's check if the browser supports notifications
                    if (!("Notification" in window)) {
                        resolve("FeatureNotSupported");
                    }
                    else {
                        if (checkNotificationPromise()) {
                            Notification.requestPermission().then((permission) => {
                                handlePermission(permission);
                            });
                        }
                        else {
                            Notification.requestPermission(function (permission) {
                                handlePermission(permission);
                            });
                        }
                    }
                });
            }
            static GetButtonLimit() {
                return Notification.maxActions;
            }
            static ToastNotificationImplementation() {
                this.SetNotificationClickHandler("[ToastNotification.Wasm] Uno.Extras.ToastNotificationImplementation:OnNotificationClick");
                navigator.serviceWorker.addEventListener("message", (ev) => {
                    if (ev.data.op) {
                        switch (ev.data.op) {
                            case "CHECK_GUID":
                                var otherGuid = ev.data.payload;
                                if (otherGuid === this._guid) {
                                    ev.ports[0].postMessage({ op: "RESPOND_GUID", payload: true });
                                }
                                else {
                                    ev.ports[0].postMessage({ op: "RESPOND_GUID", payload: false });
                                }
                                break;
                            case "notificationclick":
                                // Using lastIndexOf for better performance.
                                if (this._handledEvents.lastIndexOf(ev.data.payload.seq) === -1) {
                                    // Prevents further badgering...
                                    this._handledEvents.push(ev.data.payload.seq);
                                    this._callbackFunc(ev.data.payload.arg);
                                }
                                var port = ev.ports[0];
                                if (port) {
                                    port.postMessage({ op: "ACK", payload: ev.data.payload.seq });
                                }
                                // To ensure performance: We should discard some earlier events.
                                if (this._handledEvents.length > 1000) {
                                    this._handledEvents = this._handledEvents.slice(-100, -1);
                                }
                                break;
                        }
                    }
                });
            }
        }
        ToastNotificationImplementation.ServiceWorkerName = "Notifications.ServiceWorker.js";
        ToastNotificationImplementation._doneInit = false;
        ToastNotificationImplementation._handledEvents = [];
        Extras.ToastNotificationImplementation = ToastNotificationImplementation;
    })(Extras = Uno.Extras || (Uno.Extras = {}));
})(Uno || (Uno = {}));
// Invokes the static constructor.
Uno.Extras.ToastNotificationImplementation.ToastNotificationImplementation();
