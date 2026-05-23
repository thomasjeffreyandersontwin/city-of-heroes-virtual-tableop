import json
g = json.load(open('docs/stories/story-graph.json', encoding='utf-8'))
for epic in g['epics']:
    if 'Manage Characters' in epic['name']:
        for se in epic.get('sub_epics', []):
            print(f'  SubEpic: {se["name"]}')
            for s in se.get('stories', []):
                print(f'    Story: {s["name"]}')
