import json, sys
g = json.loads(open('docs/stories/story-graph.json', encoding='utf-8').read())
def walk(node, indent=0):
    name = node.get('name','?')
    ntype = node.get('node_type','?')
    print('  '*indent + '[' + ntype + '] ' + name)
    for se in node.get('sub_epics',[]): walk(se, indent+1)
    for sg in node.get('story_groups',[]): walk(sg, indent+1)
    for s in node.get('stories',[]): walk(s, indent+1)
for e in g.get('epics',[]): walk(e)
