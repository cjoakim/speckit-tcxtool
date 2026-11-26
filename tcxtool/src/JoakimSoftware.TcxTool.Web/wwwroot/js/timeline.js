// The <tcx-timeline> element (specs/007-timeline-page/contracts/timeline-data.md, research.md R8,
// R10, R11): the state bar with its time axis and popup, and the three charts under it. The server
// computes every position as a fraction of the bar (TimelineLayout); this
// module only scales those fractions to the element's width and draws them with D3. It holds no
// colors (the state-* classes in app.css have them) and no network URLs. It is loaded on every page
// and does nothing where there is no <tcx-timeline>, so it also works after enhanced navigation.

const marginLeft = 56;
const marginRight = 16;
const barHeight = 32;
const axisHeight = 24;
const chartHeight = 140;
const chartPadding = 8;
const pointRadius = 2.5;
const minHitWidth = 4;
const popupOffset = 12;
const knownStates = new Set(["stopped", "walking", "running", "unknown"]);

let d3Loading = null;

// The vendored UMD bundle defines the d3 global. It is fetched once, the first time a timeline is
// connected, so other pages never load it (tasks.md T031 done note).
function loadD3() {
    if (globalThis.d3) {
        return Promise.resolve(globalThis.d3);
    }

    d3Loading ??= new Promise((resolve, reject) => {
        const script = document.createElement("script");
        script.src = new URL("../lib/d3/d3.min.js", import.meta.url).href;
        script.onload = () => resolve(globalThis.d3);
        script.onerror = () => {
            d3Loading = null;
            script.remove();
            reject(new Error("The timeline could not load D3."));
        };
        document.head.append(script);
    });
    return d3Loading;
}

function stateClass(state) {
    return "state-" + (knownStates.has(state) ? state : "unknown");
}

function clock(seconds) {
    const total = Math.max(0, Math.round(seconds));
    const hours = Math.floor(total / 3600);
    const minutes = Math.floor((total % 3600) / 60);
    const rest = total % 60;
    return `${hours}:${String(minutes).padStart(2, "0")}:${String(rest).padStart(2, "0")}`;
}

class TcxTimeline extends HTMLElement {
    static observedAttributes = ["data-timeline"];

    #d3 = null;
    #layout = null;
    #width = -1;
    #plot = 0;
    #observer = null;
    #mutations = null;
    #popup = null;
    #shown = -1;
    #onDocumentClick = (event) => {
        if (this.#popup && !this.#popup.contains(event.target)) {
            this.#hidePopup();
        }
    };

    connectedCallback() {
        document.addEventListener("click", this.#onDocumentClick);
        this.#observer = new ResizeObserver(() => {
            if (this.#measure() !== this.#width) {
                this.#draw();
            }
        });
        this.#observer.observe(this);
        // An enhanced navigation to the same page merges the server's empty containers into this
        // element, which removes the drawn SVGs without changing data-timeline (tasks.md T048).
        this.#mutations = new MutationObserver((records) => {
            if (records.some((record) => [...record.removedNodes].some(TcxTimeline.#holdsSvg))) {
                this.#draw();
            }
        });
        this.#mutations.observe(this, { childList: true, subtree: true });
        loadD3().then((d3) => {
            this.#d3 = d3;
            this.#draw();
        }, (error) => console.error(error));
    }

    attributeChangedCallback() {
        if (this.isConnected) {
            this.#draw();
        }
    }

    disconnectedCallback() {
        document.removeEventListener("click", this.#onDocumentClick);
        this.#observer?.disconnect();
        this.#observer = null;
        this.#mutations?.disconnect();
        this.#mutations = null;
    }

    static #holdsSvg(node) {
        return node.nodeName.toLowerCase() === "svg" || (node instanceof Element && node.querySelector("svg") !== null);
    }

    #measure() {
        return Math.floor(this.getBoundingClientRect().width);
    }

    #draw() {
        const d3 = this.#d3;
        if (!d3) {
            return;
        }

        try {
            this.#layout = JSON.parse(this.dataset.timeline ?? "");
        } catch {
            this.#layout = null;
        }

        const bar = this.querySelector(":scope > .timeline-bar");
        if (!bar || !this.#layout || !Array.isArray(this.#layout.segments) || this.#layout.segments.length === 0) {
            return;
        }

        this.#width = this.#measure();
        this.#plot = Math.max(0, this.#width - marginLeft - marginRight);
        this.#hidePopup();
        this.#drawBar(d3, bar);
        this.#drawCharts(d3);
        // Drop the records of this draw's own removals, so they don't trigger another draw.
        this.#mutations?.takeRecords();
    }

    #x(fraction) {
        return marginLeft + fraction * this.#plot;
    }

    #drawBar(d3, bar) {
        const layout = this.#layout;
        d3.select(bar).selectChildren("svg").remove();
        const svg = d3.select(bar).append("svg")
            .attr("width", this.#width)
            .attr("height", barHeight + axisHeight)
            .attr("role", "img")
            .attr("aria-label", "States over time");

        svg.append("g").selectAll("rect").data(layout.segments).join("rect")
            .attr("class", (segment) => "timeline-segment " + stateClass(segment.state))
            .attr("x", (segment) => this.#x(segment.start))
            .attr("y", 0)
            .attr("width", (segment) => Math.max(0, (segment.end - segment.start) * this.#plot))
            .attr("height", barHeight);

        const time = d3.scaleLinear([0, layout.totalSeconds], [marginLeft, marginLeft + this.#plot]);
        svg.append("g")
            .attr("transform", `translate(0,${barHeight})`)
            .call(d3.axisBottom(time).ticks(Math.max(2, Math.floor(this.#plot / 90))).tickFormat(clock));

        svg.on("pointermove", (event) => this.#hover(d3, event, svg.node()));
    }

    // Each chart shares the bar's width and margins, so its points sit under their segments' centers
    // (contracts/timeline-data.md "Charts"). A chart with no points has no container to draw into.
    #drawCharts(d3) {
        const charts = Array.isArray(this.#layout.charts) ? this.#layout.charts : [];
        for (const container of this.querySelectorAll(":scope > .timeline-chart")) {
            d3.select(container).selectChildren("svg").remove();
            const chart = charts[Number(container.dataset.chart)];
            if (!chart || !Array.isArray(chart.points) || chart.points.length === 0 || chart.min == null || chart.max == null) {
                continue;
            }

            const y = d3.scaleLinear([chart.min, chart.max], [chartHeight - chartPadding, chartPadding]);
            const svg = d3.select(container).append("svg")
                .attr("width", this.#width)
                .attr("height", chartHeight)
                .attr("role", "img")
                .attr("aria-label", chart.label);

            svg.append("g")
                .attr("transform", `translate(${marginLeft},0)`)
                .call(d3.axisLeft(y).ticks(4));

            const line = d3.line()
                .x((point) => this.#x(point.center))
                .y((point) => y(point.value));
            svg.append("path")
                .attr("class", "timeline-line")
                .attr("d", line(chart.points));

            svg.append("g").selectAll("circle").data(chart.points).join("circle")
                .attr("class", "timeline-point")
                .attr("cx", (point) => this.#x(point.center))
                .attr("cy", (point) => y(point.value))
                .attr("r", pointRadius);
        }
    }

    // Only the 32 px bar opens the popup, not the time axis under it. A segment under 4 px wide whose
    // center is within 4 px of the pointer wins, the nearest first and the earlier on a tie, even over
    // a wide segment, so zero-width segments can be reached. Otherwise the segment holding the
    // pointer's fraction is found by bisecting the starts (contracts/timeline-data.md "Popup").
    #hover(d3, event, svg) {
        const segments = this.#layout.segments;
        const [pointer, height] = d3.pointer(event, svg);
        if (this.#plot <= 0 || height < 0 || height >= barHeight || pointer < marginLeft || pointer > marginLeft + this.#plot) {
            return;
        }

        let index = -1;
        let nearestDistance = Infinity;
        for (let i = 0; i < segments.length; i++) {
            if ((segments[i].end - segments[i].start) * this.#plot >= minHitWidth) {
                continue;
            }

            const distance = Math.abs(this.#x(segments[i].center) - pointer);
            if (distance <= minHitWidth && distance < nearestDistance) {
                index = i;
                nearestDistance = distance;
            }
        }

        if (index < 0) {
            const fraction = (pointer - marginLeft) / this.#plot;
            index = Math.min(segments.length - 1, Math.max(0, d3.bisector((segment) => segment.start).right(segments, fraction) - 1));
        }

        this.#showPopup(index, pointer);
    }

    #showPopup(index, pointer) {
        const popup = this.#ensurePopup();
        if (index !== this.#shown) {
            const list = popup.querySelector("dl");
            list.replaceChildren();
            const { fieldNames } = this.#layout;
            const values = this.#layout.segments[index].values;
            for (let i = 0; i < fieldNames.length; i++) {
                const term = document.createElement("dt");
                term.className = "col-7 fw-normal text-muted";
                term.textContent = fieldNames[i];
                const value = document.createElement("dd");
                value.className = "col-5 mb-0 text-break";
                value.textContent = values[i] ?? "";
                list.append(term, value);
            }

            this.#shown = index;
        }

        popup.hidden = false;
        const width = popup.offsetWidth;
        let left = pointer + popupOffset;
        if (left + width > this.#width) {
            left = Math.max(0, pointer - popupOffset - width);
        }

        popup.style.left = `${left}px`;
        popup.style.top = `${barHeight + 4}px`;
    }

    #ensurePopup() {
        if (!this.#popup || !this.#popup.isConnected) {
            const popup = document.createElement("div");
            popup.className = "timeline-popup card small shadow";
            popup.setAttribute("role", "tooltip");
            popup.hidden = true;
            const body = document.createElement("div");
            body.className = "card-body p-2";
            const list = document.createElement("dl");
            list.className = "row mb-0";
            body.append(list);
            popup.append(body);
            this.append(popup);
            this.#popup = popup;
            this.#shown = -1;
        }

        return this.#popup;
    }

    #hidePopup() {
        if (this.#popup) {
            this.#popup.hidden = true;
        }
    }
}

if (!customElements.get("tcx-timeline")) {
    customElements.define("tcx-timeline", TcxTimeline);
}
