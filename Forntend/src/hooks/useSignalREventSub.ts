import { useEffect } from "react";
import signalRService from "../services/signalRService";
import { JobEvent } from "../modals/JobEvent";

function useSignalRSubscription<T>(
  event: JobEvent,
  handler: (payload: T) => void
) {
  useEffect(() => {
    const unsubscribe = signalRService.subscribeToEvent<T>(event, handler);

    return () => {
      unsubscribe();
    };
  }, [event, handler]);
}

export default useSignalRSubscription;
