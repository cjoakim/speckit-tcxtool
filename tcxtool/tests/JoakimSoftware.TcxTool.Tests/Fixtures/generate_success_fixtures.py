"""Writes synthetic TCX fixtures and their golden CSVs (features 002 and 006).

Golden values are computed here, independently of the C# code under test:
derived numbers use Python's shortest round-trip repr, expanded to plain decimals.
State and StateSeconds follow specs/006-activity-states/contracts/csv-format.md, with
durations computed as timedeltas.
Usage (from tcxtool/): python3 -I tests/JoakimSoftware.TcxTool.Tests/Fixtures/generate_success_fixtures.py \
    tests/JoakimSoftware.TcxTool.Tests/Fixtures run-two-laps ride-positions ...
"""
import sys
from datetime import datetime, timedelta
from decimal import Decimal
from pathlib import Path

TCX = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"
EXT = "http://www.garmin.com/xmlschemas/ActivityExtension/v2"
HEADER = ("ActivityId|Date|DeviceName|DeviceId|Sport|TrackpointNum|Time|ElapsedSeconds|ElapsedTime|"
          "Latitude|Longitude|HeartRateBpm|RunCadence|State|StateSeconds|DistanceMeters|DistanceMiles|"
          "AltitudeMeters|AltitudeFeet")
STATES_HEADER = ("ActivityId|ActivityDistanceMiles|ActivityElapsedTime|ActivityElapsedSeconds|State|StateEndDate|"
                 "StateEndTrackpointNum|StateTrackpointCount|StateElapsedSeconds|StateDistanceMiles|"
                 "AverageHeartRateBpm|AverageRunCadence|AverageLatitude|AverageLongitude|AverageAltitudeMeters|"
                 "AverageAltitudeFeet")
RUNNING_STEPS_PER_MINUTE = 150
# Activity CSV field positions (0-based) used by the States CSV.
F_ID, F_NUM, F_TIME, F_ELAPSED_SECONDS, F_ELAPSED_TIME = 0, 5, 6, 7, 8
F_LAT, F_LON, F_HR, F_CAD, F_STATE, F_STATE_SECONDS = 9, 10, 11, 12, 13, 14
F_MILES, F_ALT_M, F_ALT_FT = 16, 17, 18


def fmt(x):
    if x == 0:
        return "0"
    s = format(Decimal(repr(x)), "f")
    if "." in s:
        s = s.rstrip("0").rstrip(".")
    return s


def parse_time(text):
    return datetime.fromisoformat(text.replace("Z", "+00:00"))


def clock(seconds):
    sign = "-" if seconds < 0 else ""
    whole = int(abs(seconds))  # truncate toward zero
    return f"{sign}{whole // 3600:02d}:{whole % 3600 // 60:02d}:{whole % 60:02d}"


def state(steps_per_minute):
    if steps_per_minute is None or steps_per_minute < 0:
        return "unknown"
    if steps_per_minute == 0:
        return "stopped"
    return "walking" if steps_per_minute < RUNNING_STEPS_PER_MINUTE else "running"


def clean(v):
    return (v or "").replace("|", " ").replace("\r", "").replace("\n", "")


# A trackpoint is a dict: time, lat, lon, alt, dist, hr, cad, speed, watts (all optional text)
def tp_xml(tp, extra=""):
    out = ["          <Trackpoint>"]
    if tp.get("time") is not None:
        out.append(f"            <Time>{tp['time']}</Time>")
    if tp.get("lat") is not None or tp.get("lon") is not None:
        out.append("            <Position>")
        if tp.get("lat") is not None:
            out.append(f"              <LatitudeDegrees>{tp['lat']}</LatitudeDegrees>")
        if tp.get("lon") is not None:
            out.append(f"              <LongitudeDegrees>{tp['lon']}</LongitudeDegrees>")
        out.append("            </Position>")
    if tp.get("alt") is not None:
        out.append(f"            <AltitudeMeters>{tp['alt']}</AltitudeMeters>")
    if tp.get("dist") is not None:
        out.append(f"            <DistanceMeters>{tp['dist']}</DistanceMeters>")
    if tp.get("hr") is not None:
        out.append(f"            <HeartRateBpm>\n              <Value>{tp['hr']}</Value>\n            </HeartRateBpm>")
    if any(tp.get(k) is not None for k in ("speed", "cad", "watts")) or extra:
        out.append("            <Extensions>")
        if any(tp.get(k) is not None for k in ("speed", "cad", "watts")):
            out.append("              <ns3:TPX>")
            if tp.get("speed") is not None:
                out.append(f"                <ns3:Speed>{tp['speed']}</ns3:Speed>")
            if tp.get("cad") is not None:
                out.append(f"                <ns3:RunCadence>{tp['cad']}</ns3:RunCadence>")
            if tp.get("watts") is not None:
                out.append(f"                <ns3:Watts>{tp['watts']}</ns3:Watts>")
            out.append("              </ns3:TPX>")
        if extra:
            out.append(extra)
        out.append("            </Extensions>")
    for raw in tp.get("raw", []):
        out.append(raw)
    out.append("          </Trackpoint>")
    return "\n".join(out)


def activity_xml(act):
    out = [f'    <Activity Sport="{act["sport"]}">' if act.get("sport") else "    <Activity>"]
    if act.get("id") is not None:
        out.append(f"      <Id>{act['id']}</Id>")
    for lap in act["laps"]:
        out.append(f'      <Lap StartTime="{lap["start"]}">')
        out.append(f"        <TotalTimeSeconds>{lap.get('total', '10.0')}</TotalTimeSeconds>")
        out.append(f"        <DistanceMeters>{lap.get('dist', '100.0')}</DistanceMeters>")
        out.append("        <Calories>12</Calories>")
        out.append("        <Intensity>Active</Intensity>")
        out.append("        <TriggerMethod>Manual</TriggerMethod>")
        if lap.get("extra"):
            out.append(lap["extra"])
        if lap.get("tps") is not None:
            out.append("        <Track>")
            out.extend(tp_xml(tp, tp.get("extra", "")) for tp in lap["tps"])
            out.append("        </Track>")
        out.append("      </Lap>")
    if act.get("creator"):
        name, unit = act["creator"]
        out.append('      <Creator xsi:type="Device_t">')
        out.append(f"        <Name>{name}</Name>")
        out.append(f"        <UnitId>{unit}</UnitId>")
        out.append("        <ProductID>9999</ProductID>")
        out.append("      </Creator>")
    out.append("    </Activity>")
    return "\n".join(out)


def document(acts):
    body = "\n".join(activity_xml(a) for a in acts)
    return f'''<?xml version="1.0" encoding="UTF-8"?>
<TrainingCenterDatabase
  xsi:schemaLocation="{TCX} http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd"
  xmlns:ns3="{EXT}"
  xmlns="{TCX}"
  xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Activities>
{body}
  </Activities>
  <Author xsi:type="Application_t">
    <Name>Synthetic fixture</Name>
  </Author>
</TrainingCenterDatabase>
'''


def activity_fields(activity_id, act):
    date = act["id"][:10]
    name, unit = act.get("creator") or (None, None)
    tps = [tp for lap in act["laps"] for tp in (lap.get("tps") or [])]
    ref = next((parse_time(tp["time"]) for tp in tps if tp.get("time") is not None), None)
    lines = []
    previous_state, last_timed, start = None, None, timedelta(0)
    for n, tp in enumerate(tps, start=1):
        cad = tp.get("cad")
        doubled = float(cad) * 2 if cad is not None else None
        st = state(doubled)
        # A new StateSet starts where the previous set's last timed row was, or at 0.
        if previous_state is not None and st != previous_state:
            start = last_timed if last_timed is not None else timedelta(0)
        previous_state = st
        t = tp.get("time")
        if t is not None:
            elapsed = parse_time(t) - ref
            secs = elapsed.total_seconds()
            es, et = fmt(secs), clock(secs)
            ss = fmt((elapsed - start).total_seconds())
            last_timed = elapsed
        else:
            es = et = ss = ""
        has_pos = tp.get("lat") is not None and tp.get("lon") is not None
        lat = tp["lat"] if has_pos else ""
        lon = tp["lon"] if has_pos else ""
        dist, alt = tp.get("dist"), tp.get("alt")
        fields = [activity_id, date, name, unit, act.get("sport"), str(n), t, es, et, lat, lon, tp.get("hr"),
                  fmt(doubled) if doubled is not None else "", st, ss,
                  dist, fmt(float(dist) / 1609.344) if dist else "",
                  alt, fmt(float(alt) / 0.3048) if alt else ""]
        lines.append([clean(f) for f in fields])
    return lines


def rows(activity_id, act):
    lines = [HEADER] + ["|".join(f) for f in activity_fields(activity_id, act)]
    return "".join(line + "\n" for line in lines)


def last_value(rows_, index):
    for r in reversed(rows_):
        if r[index] != "":
            return r[index]
    return ""


def mean(rows_, index):
    # An explicit left-to-right loop: never sum(), which is compensated in Python 3.12 and later.
    total, count = 0.0, 0
    for r in rows_:
        if r[index] != "":
            total += float(r[index])
            count += 1
    return fmt(total / count) if count else ""


def states_rows(fields):
    sets, out = [], [STATES_HEADER]
    for r in fields:
        if sets and sets[-1][-1][F_STATE] == r[F_STATE]:
            sets[-1].append(r)
        else:
            sets.append([r])
    before = []
    for group in sets:
        last_miles = last_value(group, F_MILES)
        before_miles = last_value(before, F_MILES)
        miles = fmt(float(last_miles) - (float(before_miles) if before_miles else 0.0)) if last_miles else ""
        # The 16 fields of specs/007-timeline-page/contracts/states-csv-format.md, in order.
        out.append("|".join([group[0][F_ID], last_miles, last_value(group, F_ELAPSED_TIME),
                             last_value(group, F_ELAPSED_SECONDS), group[0][F_STATE], last_value(group, F_TIME),
                             group[-1][F_NUM], str(len(group)), last_value(group, F_STATE_SECONDS), miles,
                             mean(group, F_HR), mean(group, F_CAD), mean(group, F_LAT), mean(group, F_LON),
                             mean(group, F_ALT_M), mean(group, F_ALT_FT)]))
        before.extend(group)
    return "".join(line + "\n" for line in out)


def write(root, name, acts, golden):
    (root / f"{name}.tcx").write_text(document(acts), encoding="utf-8", newline="\n")
    (root / "Expected").mkdir(exist_ok=True)
    for file_name, act in golden:
        (root / "Expected" / file_name).write_text(rows(name, act), encoding="utf-8", newline="\n")
        states_name = file_name[:-len(".csv")] + ".states.csv"
        (root / "Expected" / states_name).write_text(states_rows(activity_fields(name, act)), encoding="utf-8",
                                                     newline="\n")


def run_two_laps():
    times = ["2026-11-01T12:04:07.000Z", "2026-11-01T12:04:08.000Z", "2026-11-01T12:04:09.000Z",
             "2026-11-01T12:04:10.000Z", "2026-11-01T12:04:11.000Z", "2026-11-01T12:04:12.000Z",
             "2026-11-01T12:04:13.000Z", "2026-11-01T12:04:14.000Z", "2026-11-01T12:04:15.000Z",
             "2026-11-01T13:06:12.000Z"]
    dists = ["0.0", "1.5199999809265137", "2.990000009536743", "4.5", "6.019999980926514",
             "7.510000228881836", "9.0", "10.479999542236328", "12.0", "10000.0"]
    alts = ["103.5999984741211", "103.80000305175781", "103.80000305175781", "104.0", "104.19999694824219",
            "104.4000015258789", "104.5999984741211", "104.80000305175781", "105.0", "97.0"]
    tps = []
    for i in range(10):
        tps.append({"time": times[i], "lat": f"10.00{i:02d}12345678901", "lon": f"-20.00{i:02d}98765432101",
                    "alt": alts[i], "dist": dists[i], "hr": str(83 + i), "cad": str(59 + i),
                    "speed": f"1.{i}53999948501587", "watts": str(130 + i)})
    tps[3]["extra"] = '              <x:Unknown xmlns:x="urn:example:unknown">42</x:Unknown>'
    act = {"sport": "Running", "id": "2026-11-01T12:04:07.000Z", "creator": ("Forerunner 999", "1234567890"),
           "laps": [{"start": times[0], "tps": tps[:5],
                     "extra": '        <y:Note xmlns:y="urn:example:other">ignored</y:Note>'},
                    {"start": times[5], "tps": tps[5:]}]}
    return [act], [("run-two-laps.csv", act)]


def ride_positions():
    tps = [{"time": f"2026-10-15T08:00:{s:02d}Z", "lat": f"-33.86{s:02d}", "lon": f"151.21{s:02d}",
            "alt": f"{20 + s}.5", "dist": f"{s * 7}.25", "hr": str(110 + s)} for s in range(4)]
    act = {"sport": "Biking", "id": "2026-10-15T08:00:00Z", "creator": ("Edge 1040", "555"),
           "laps": [{"start": "2026-10-15T08:00:00Z", "tps": tps}]}
    return [act], [("ride-positions.csv", act)]


def multi_activity():
    def act(start_hms, base):
        tps = [{"time": f"2026-11-01T{start_hms[:-2]}{int(start_hms[-2:]) + s:02d}.000Z", "lat": f"1.{base}{s}",
                "lon": f"2.{base}{s}", "alt": f"5{s}.0", "dist": f"{s}.0", "hr": str(90 + s), "cad": "80"}
               for s in range(3)]
        return {"sport": "Running", "id": f"2026-11-01T{start_hms}.000Z", "creator": ("Forerunner 999", "77"),
                "laps": [{"start": f"2026-11-01T{start_hms}.000Z", "tps": tps}]}
    a, b = act("07:15:00", 1), act("18:30:05", 2)
    return [a, b], [("2026-11-01_071500_multi-activity.csv", a), ("2026-11-01_183005_multi-activity.csv", b)]


def fractional_times():
    times = ["2026-11-01T21:30:00.000-05:00", "2026-11-01T21:30:01.600-05:00", "2026-11-01T21:29:56.500-05:00",
             "2026-11-01T22:32:05.600-05:00", "2026-11-01T21:30:00.250-05:00"]
    tps = [{"time": t, "lat": f"0.5{i}", "lon": f"0.6{i}", "alt": f"{i}.125", "dist": f"{i * 3}.0",
            "hr": str(100 + i), "cad": str(85 + i)} for i, t in enumerate(times)]
    act = {"sport": "Running", "id": times[0], "creator": ("Edge|1040", "42"),
           "laps": [{"start": times[0], "tps": tps}]}
    return [act], [("fractional-times.csv", act)]


def missing_optional():
    tps = [
        {"lat": "3.1", "lon": "4.1", "alt": "10.0", "dist": "0.0", "hr": "90", "cad": "80"},           # no time
        {"time": "2026-11-01T06:00:00.000Z", "lat": "3.2", "lon": "4.2", "alt": "10.5", "dist": "1.0",
         "cad": "81"},                                                                                  # no HR
        {"time": "2026-11-01T06:00:01.000Z", "alt": "11.0", "dist": "2.0", "hr": "92", "cad": "82"},    # no Position
        {"time": "2026-11-01T06:00:02.000Z", "lat": "3.4", "alt": "11.5", "dist": "3.0", "hr": "93",
         "cad": "83"},                                                                                  # half Position
        {"time": "2026-11-01T06:00:03.000Z", "lat": "3.5", "lon": "4.5", "alt": "12.0", "dist": "4.0",
         "hr": "94"},                                                                                   # no Extensions
        {"time": "2026-11-01T06:00:04.000Z", "lat": "3.6", "lon": "4.6", "dist": "5.0", "hr": "95",
         "cad": "85"},                                                                                  # no altitude
        {"time": "2026-11-01T06:00:05.000Z", "lat": "3.7", "lon": "4.7", "alt": "13.0", "hr": "96",
         "cad": "86"},                                                                                  # no distance
        {"time": "2026-11-01T06:00:06.000Z", "lat": "3.8", "lon": "4.8", "dist": "7.0", "hr": "97",
         "cad": "87", "raw": ["            <AltitudeMeters></AltitudeMeters>"]},                      # empty altitude
    ]
    act = {"sport": "Running", "id": "2026-11-01T06:00:00.000Z", "laps": [{"start": "2026-11-01T06:00:00.000Z", "tps": tps}]}
    return [act], [("missing-optional.csv", act)]


def no_trackpoints():
    act = {"sport": "Running", "id": "2026-11-01T05:00:00.000Z", "creator": ("Forerunner 999", "1"),
           "laps": [{"start": "2026-11-01T05:00:00.000Z", "tps": []}, {"start": "2026-11-01T05:10:00.000Z"}]}
    return [act], [("no-trackpoints.csv", act)]


def run_states():
    # One row per tuple: time (None = untimed), TCX cadence (None = missing), distance (None = missing).
    plan = [("06:00:00.000", "0", "0.0"), ("06:00:01.000", "0", "1.0"),        # stopped
            ("06:00:02.500", "1", "2.5"), ("06:00:03.500", "74", "4.0"),       # walking
            (None, "74", "5.5"),                                               # walking, untimed
            ("06:00:04.500", "75", "7.25"), ("06:00:05.500", "90", "9.0"),     # running
            ("06:00:06.500", None, "11.0"),                                    # unknown, one row
            ("06:00:10.000", "80", "15.5"), ("06:00:11.000", "82", "18.0"),    # running, after a gap
            ("06:00:12.300", "60", None), ("06:00:13.300", "60", None),        # walking, no distance
            ("06:00:14.300", "0", "21.75"), ("06:00:15.300", "0", "22.0")]     # stopped
    hrs = ["95", "96", "100", "104", "105", "120", "130", "131", None, "140", "110", "108", "99", "97"]
    tps = []
    for i, (clock_time, cad, dist) in enumerate(plan):
        tp = {"lat": f"40.10{i:02d}1", "lon": f"-75.20{i:02d}7", "alt": f"{30 + i}.5", "dist": dist,
              "hr": hrs[i], "cad": cad}
        if clock_time is not None:
            tp["time"] = f"2026-11-01T{clock_time}Z"
        tps.append(tp)
    tps[6]["lat"] = tps[6]["lon"] = None    # no position
    tps[3]["alt"] = None                    # no altitude
    act = {"sport": "Running", "id": "2026-11-01T06:00:00.000Z", "creator": ("Forerunner 999", "4242"),
           "laps": [{"start": "2026-11-01T06:00:00.000Z", "tps": tps[:7]},
                    {"start": "2026-11-01T06:00:06.500Z", "tps": tps[7:]}]}
    return [act], [("run-states.csv", act)]


BUILDERS = {"run-two-laps": run_two_laps, "ride-positions": ride_positions, "multi-activity": multi_activity,
            "fractional-times": fractional_times, "missing-optional": missing_optional,
            "no-trackpoints": no_trackpoints, "run-states": run_states}

if __name__ == "__main__":
    root = Path(sys.argv[1])
    for fixture in sys.argv[2:]:
        acts, golden = BUILDERS[fixture]()
        write(root, fixture, acts, golden)
        print("wrote", fixture)
