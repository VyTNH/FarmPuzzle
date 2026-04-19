import json

manifest_path = r'd:\UnityProjects\farmpuzzle\Packages\manifest.json'
with open(manifest_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

if 'com.unity.inputsystem' in data.get('dependencies', {}):
    del data['dependencies']['com.unity.inputsystem']
    with open(manifest_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2)
    print("Successfully removed com.unity.inputsystem")
else:
    print("com.unity.inputsystem not found in manifest")
