using System;
using System.Collections.Generic;
using Drone.Contracts;
using Drone.Core.Session;
using UnityEngine;

namespace Drone.App {
    
    public class DroneClients {
        
        private readonly List<IDroneClient> _clients = new();
        private GameObject _root;
        private DroneSession _session;
        
        public void AttachAll(GameObject root, DroneSession session) {
            _root = root;
            _session = session;
            
            foreach (var frontend in root.GetComponentsInChildren<IDroneFrontend>()) {
                try {
                    frontend.Attach(_session);
                    _clients.Add(frontend);
                }
                catch (Exception exception) {
                    Debug.LogException(exception, root);
                    DetachSafely(frontend);
                }
            }

            foreach (var display in root.GetComponentsInChildren<IDroneDisplay>()) {
                try {
                    display.Attach(_session.Readout);
                    _clients.Add(display);
                }
                catch (Exception exception) {
                    Debug.LogException(exception, root);
                    DetachSafely(display);
                }
            }
        }

        public void DetachAll() {
            foreach (var client in _clients) {
                DetachSafely(client);
            }
            _clients.Clear();
            _session?.Dispose();
        }

        private void DetachSafely(IDroneClient client) {

            try {
                client.Detach();
            }
            catch (Exception exception) {
                Debug.LogException(exception, _root);
            }
        }
    }
}