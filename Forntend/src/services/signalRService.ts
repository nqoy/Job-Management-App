import * as signalR from "@microsoft/signalR";
import { JobProgressUpdate } from "../modals/Job";
import { JobEvent } from "../modals/JobEvent";

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private progressUpdateCallbacks: ((update: JobProgressUpdate) => void)[] = [];

  async startConnection() {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/JobSignalRHub?service=JobsApp")
      .withAutomaticReconnect()
      .build();

    this.connection.on(
      JobEvent.UpdateJobProgress,
      (updatedProgress: JobProgressUpdate) => {
        this.progressUpdateCallbacks.forEach((callback) =>
          callback(updatedProgress)
        );
      }
    );

    try {
      await this.connection.start();
      console.log("SignalR Connected");
    } catch (err) {
      console.error("SignalR Connection Error: ", err);
      setTimeout(() => this.startConnection(), 5000);
    }
  }

  subscribeToEvent(event: JobEvent, callback: (update: any) => void) {
    this.progressUpdateCallbacks.push(callback);

    this.connection?.on(event, callback);

    const unsubscribe = () => {
      this.connection?.off(event, callback);
      this.progressUpdateCallbacks = this.progressUpdateCallbacks.filter(
        (existingCallback) => existingCallback !== callback
      );
    };

    return unsubscribe;
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
