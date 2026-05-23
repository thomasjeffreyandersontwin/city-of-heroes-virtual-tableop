import json

def show_node(node, indent=0):
    prefix = "  " * indent
    # stories at this level
    for story in node.get("stories", []):
        print(f"{prefix}  (S) {story['name']}")
    # sub-epics
    for se in node.get("sub_epics", []):
        print(f"{prefix}(E) {se['name']}")
        show_node(se, indent + 1)

g = json.load(open('docs/stories/story-graph.json', encoding='utf-8'))
for epic in g['epics']:
    print(f"(E) {epic['name']}  [stories={len(epic.get('stories',[]))}]")
    show_node(epic, 1)
