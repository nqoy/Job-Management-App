import * as signalR from "@microsoft/signalR";
import { JobProgressUpdate } from "../modals/Job";

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private progressUpdateCallbacks: ((update: JobProgressUpdate) => void)[] = [];

  async startConnection() {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/JobSignalRHub?service=JobsApp")
      .withAutomaticReconnect()
      .build();

    this.connection.on("UpdateJobProgress", (update: JobProgressUpdate) => {
      this.progressUpdateCallbacks.forEach((callback) => callback(update));
    });

    try {
      await this.connection.start();
      console.log("SignalR Connected");
    } catch (err) {
      console.error("SignalR Connection Error: ", err);
      setTimeout(() => this.startConnection(), 5000);
    }
  }

  onJobProgressUpdate(callback: (update: JobProgressUpdate) => void) {
    this.progressUpdateCallbacks.push(callback);
    return () => {
      this.progressUpdateCallbacks = this.progressUpdateCallbacks.filter(
        (cb) => cb !== callback
      );
    };
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
