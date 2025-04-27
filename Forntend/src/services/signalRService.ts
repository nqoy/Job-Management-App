import * as signalR from "@microsoft/signalR";
import { JobEvent } from "../modals/JobEvent";

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private eventHandlers: { [event: string]: ((update: any) => void)[] } = {};

  async startConnection() {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/JobSignalRHub?service=JobsApp")
      .withAutomaticReconnect()
      .build();

    try {
      await this.connection.start();
      console.log("SignalR Connected");
    } catch (err) {
      console.error("SignalR Connection Error: ", err);
      setTimeout(() => this.startConnection(), 5000);
    }
  }

  subscribeToEvent<T>(event: JobEvent, callback: (payload: T) => void) {
    if (!this.eventHandlers[event]) {
      this.eventHandlers[event] = [];
    }
    this.eventHandlers[event].push(callback);

    if (this.connection) {
      this.connection.on(event, (payload: T) => {
        this.registerEventToHandler(event, payload);
      });
    }

    const unsubscribe = () => {
      this.eventHandlers[event] = this.eventHandlers[event].filter(
        (existingCallback) => existingCallback !== callback
      );
      if (this.eventHandlers[event].length === 0) {
        this.connection?.off(event);
      }
    };

    return unsubscribe;
  }

  private registerEventToHandler<T>(event: JobEvent, payload: T) {
    if (this.eventHandlers[event]) {
      this.eventHandlers[event].forEach((handler) => handler(payload));
    }
  }

  async stopConnection() {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}

export const signalRService = new SignalRService();
export default signalRService;
