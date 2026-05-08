import { useEffect, useMemo, useRef, useState } from "react";
import $ from "jquery";
import "datatables.net-dt";
import "datatables.net-dt/css/dataTables.dataTables.css";
import "datatables.net-buttons-dt";
import "datatables.net-buttons-dt/css/buttons.dataTables.css";
import "datatables.net-buttons/js/buttons.html5";
import "datatables.net-buttons/js/buttons.print";
import JSZip from "jszip";
import pdfMake from "pdfmake/build/pdfmake";
import "pdfmake/build/vfs_fonts";

(window as any).JSZip = JSZip;
(pdfMake as any).vfs = (window as any).pdfMake?.vfs;
(window as any).pdfMake = pdfMake;

type AttendanceRow = {
  staff_number: string;
  name: string;
  time: string;
  office_name: string;
};

type ApiPayload = {
  data?: Record<string, unknown>[];
  count?: number;
};

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "";
const today = new Date().toISOString().slice(0, 10);
type CardKey = "punctual" | "late" | "night" | "statistics" | "absent" | "unassigned";

const getJson = async (url: string): Promise<ApiPayload> => {
  const response = await fetch(`${API_BASE}${url}`);
  if (!response.ok) {
    throw new Error(`Request failed: ${url}`);
  }
  return response.json();
};

const normalize = (rows: Record<string, unknown>[] = []): AttendanceRow[] =>
  rows.map((row) => ({
    staff_number: String(row.staff_number ?? row.staff_id ?? row.staff ?? ""),
    name: String(row.name ?? row.staff_name ?? "N/A"),
    time: String(row.time ?? row.entry_time ?? "N/A"),
    office_name: String(row.office_name ?? row.Office ?? row.Clock_in_office ?? row.region ?? "N/A")
  }));

const StatCard = ({
  title,
  value,
  active,
  onClick
}: {
  title: string;
  value: number;
  active: boolean;
  onClick: () => void;
}) => (
  <button
    type="button"
    onClick={onClick}
    className={`rounded-lg bg-white p-5 shadow-sm border text-left transition ${
      active ? "border-[#1e3a8a] ring-1 ring-[#1e3a8a]" : "border-slate-200 hover:border-blue-200"
    }`}
  >
    <p className="text-sm text-slate-500">{title}</p>
    <p className="mt-2 text-3xl font-bold text-[#1e3a8a]">{value}</p>
  </button>
);

function App() {
  const tableRef = useRef<HTMLTableElement | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [fromDate, setFromDate] = useState(today);
  const [toDate, setToDate] = useState(today);
  const [singleDate, setSingleDate] = useState(today);
  const [activeCard, setActiveCard] = useState<CardKey>("punctual");
  const [punctual, setPunctual] = useState<AttendanceRow[]>([]);
  const [late, setLate] = useState<AttendanceRow[]>([]);
  const [night, setNight] = useState<AttendanceRow[]>([]);
  const [statistics, setStatistics] = useState<AttendanceRow[]>([]);
  const [absent, setAbsent] = useState<AttendanceRow[]>([]);
  const [statsCount, setStatsCount] = useState<number>(0);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError("");
        const [punctualRes, lateRes, nightRes, statsRes, absentRes] = await Promise.all([
          getJson(`/api/v1/attendancereportpunctual?from=${fromDate}&to=${toDate}`),
          getJson(`/api/v1/attendancereportlate?from=${fromDate}&to=${toDate}`),
          getJson(`/api/v1/attendancereportnight?from=${fromDate}&to=${toDate}`),
          getJson(`/api/v1/attendancereportstatistics?from=${fromDate}&to=${toDate}`),
          getJson(`/api/v1/attendancereportabsent?date=${singleDate}`)
        ]);

        setPunctual(normalize(punctualRes.data));
        setLate(normalize(lateRes.data));
        setNight(normalize(nightRes.data));
        setStatistics(normalize(statsRes.data));
        setAbsent(normalize(absentRes.data));
        setStatsCount(Number(statsRes.count ?? statsRes.data?.length ?? 0));
      } catch {
        setError("Could not fetch attendance dashboard data.");
      } finally {
        setLoading(false);
      }
    };

    load();
  }, [fromDate, toDate, singleDate]);

  const combinedAttendance = useMemo(
    () => [...punctual, ...late, ...night],
    [punctual, late, night]
  );

  const unassignedRows = useMemo(
    () =>
      combinedAttendance.filter(
        (x) => !x.office_name || x.office_name.trim() === "" || x.office_name === "N/A"
      ),
    [combinedAttendance]
  );

  const tableData = useMemo(() => {
    switch (activeCard) {
      case "punctual":
        return punctual;
      case "late":
        return late;
      case "night":
        return night;
      case "statistics":
        return statistics;
      case "absent":
        return absent;
      case "unassigned":
        return unassignedRows;
      default:
        return [];
    }
  }, [activeCard, punctual, late, night, statistics, absent, unassignedRows]);

  useEffect(() => {
    if (!tableRef.current) return;

    const existing = $.fn.dataTable.isDataTable(tableRef.current)
      ? $(tableRef.current).DataTable()
      : null;
    if (existing) {
      existing.destroy();
      $(tableRef.current).empty();
    }

    $(tableRef.current).DataTable({
      data: tableData,
      columns: [
        { data: "name", title: "name" },
        { data: "staff_number", title: "staff_number" },
        { data: "time", title: "time" },
        { data: "office_name", title: "office_name" }
      ],
      dom: "Bfrtip",
      buttons: ["copy", "csv", "excel", "pdf", "print"],
      pageLength: 10
    });
  }, [tableData]);

  return (
    <div className="min-h-screen bg-slate-100">
      <header className="bg-[#1e3a8a] px-8 py-5 text-white shadow-sm">
        <h1 className="text-2xl font-semibold">Attendance Dashboard</h1>
      </header>

      <main className="p-6">
        {loading && <p className="text-slate-700">Loading...</p>}
        {error && <p className="text-red-600">{error}</p>}

        {!loading && !error && (
          <>
            <section className="mb-6 grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-5">
              <StatCard title="Punctual" value={punctual.length} active={activeCard === "punctual"} onClick={() => setActiveCard("punctual")} />
              <StatCard title="Late" value={late.length} active={activeCard === "late"} onClick={() => setActiveCard("late")} />
              <StatCard title="Night Shift" value={night.length} active={activeCard === "night"} onClick={() => setActiveCard("night")} />
              <StatCard title="Statistics" value={statsCount} active={activeCard === "statistics"} onClick={() => setActiveCard("statistics")} />
              <StatCard title="Absent Staff" value={absent.length} active={activeCard === "absent"} onClick={() => setActiveCard("absent")} />
              <StatCard title="Unassigned Office" value={unassignedRows.length} active={activeCard === "unassigned"} onClick={() => setActiveCard("unassigned")} />
            </section>

            <section className="rounded-lg bg-white p-4 shadow-sm">
              <div className="mb-4 flex flex-wrap items-end gap-3">
                <div>
                  <label className="mb-1 block text-xs text-slate-500">From</label>
                  <input
                    type="date"
                    value={fromDate}
                    onChange={(e) => setFromDate(e.target.value)}
                    className="rounded border border-slate-300 px-3 py-2 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs text-slate-500">To</label>
                  <input
                    type="date"
                    value={toDate}
                    onChange={(e) => setToDate(e.target.value)}
                    className="rounded border border-slate-300 px-3 py-2 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs text-slate-500">Absent Date</label>
                  <input
                    type="date"
                    value={singleDate}
                    onChange={(e) => setSingleDate(e.target.value)}
                    className="rounded border border-slate-300 px-3 py-2 text-sm"
                  />
                </div>
              </div>
              <h2 className="mb-4 text-lg font-semibold text-[#1e3a8a]">
                Staff List - {activeCard.charAt(0).toUpperCase() + activeCard.slice(1)}
              </h2>
              <div className="overflow-x-auto">
                <table ref={tableRef} className="display hover stripe w-full" />
              </div>
            </section>
          </>
        )}
      </main>
    </div>
  );
}

export default App;
