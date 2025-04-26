import JobDashboard from "./components/JobDashboard/JobDashboard";
import "./styles/global.css";
import { JobProvider } from "./context/JobContext";

function App() {
  return (
    <JobProvider>
      <div className="container">
        <JobDashboard />
      </div>
    </JobProvider>
  );
}

export default App;
