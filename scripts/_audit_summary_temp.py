from pathlib import Path
import sys
from collections import Counter
sys.path.insert(0, str(Path(".cursor/skills/drawio-domain-sync/scripts")))
from drawio_tools import audit_diagram
critical = {"class_overlap", "edge_crosses_class", "hierarchy_flow"}
for i in range(1, 7):
    f = f"docs/increment-{i}/class-diagram-increment-{i}.drawio"
    results = audit_diagram(f)
    print(f"INCREMENT {i}")
    for pname, info in results.items():
        c = Counter(r for r,_ in info["violations"] if r in critical)
        w = Counter(r for r,_ in info["violations"] if r not in critical)
        if c:
            st = "FAIL (critical)"
        elif w:
            st = "PASS*"
        else:
            st = "PASS"
        print(f"  {st}\t{pname}\t crit={dict(c)} warn={dict(w)}")
