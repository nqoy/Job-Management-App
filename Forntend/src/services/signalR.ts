import * as signalR from "@microsoft/signalR";
import { JobStatusUpdate } from "../modals/Job";

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private statusUpdateCallbacks: ((update: JobStatusUpdate) => void)[] = [];

  async startConnection() {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/jobHub")
      .withAutomaticReconnect()
      .build();

    this.connection.on("UpdateJobStatus", (update: JobStatusUpdate) => {
      this.statusUpdateCallbacks.forEach((callback) => callback(update));
    });

    try {
      await this.connection.start();
      console.log("SignalR Connected");
    } catch (err) {
      console.error("SignalR Connection Error: ", err);
      setTimeout(() => this.startConnection(), 5000);
    }
  }

  onJobStatusUpdate(callback: (update: JobStatusUpdate) => void) {
    this.statusUpdateCallbacks.push(callback);
    return () => {
      this.statusUpdateCallbacks = this.statusUpdateCallbacks.filter(
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
