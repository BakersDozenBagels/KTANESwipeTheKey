using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SwipeTheKeyScript : MonoBehaviour
{
    [SerializeField]
    private CardFolder _folder;
    [SerializeField]
    private Material[] _lightMats;
    [SerializeField]
    private Renderer _led;
    [SerializeField]
    private Texture[] _symbols;
    [SerializeField]
    private Renderer _symbol;

    private bool _unSolved = true;
    private int _id = ++_idc;
    private static int _idc;

    private int _symbolIx;

    private void Start()
    {
        EnsureFolder();

        GetComponent<KMSelectable>().OnFocus += () => { GetComponentInChildren<KeyCardAcceptor>().Active = _unSolved; };
        GetComponent<KMSelectable>().OnDefocus += () => { GetComponentInChildren<KeyCardAcceptor>().Active = false; };

        GetComponentInChildren<KeyCardAcceptor>().OnCollide += CardCollide;

        _symbolIx = Random.Range(0, 16);
        _symbol.material.mainTexture = _symbols[_symbolIx];
        Log("The displayed symbol is #" + _symbolIx);
        _symbol.transform.Rotate(transform.up, Random.Range(0f, 360f), Space.World);
    }

    private int _scanDir;
    private float _firstTime;

    private void CardCollide(Transform card)
    {
        if(card.localPosition.x < -0.5f)
            card.localPosition = new Vector3(-0.5f, card.localPosition.y, card.localPosition.z);

        if(card.localPosition.x < -0.25f)
        {
            if(card.localPosition.z > 0.5f || card.localPosition.z < -0.5f)
            {
                int newdir = card.localPosition.z > 0 ? 1 : 2;
                if(newdir == 1 && _scanDir == 2 || newdir == 2 && _scanDir == 1)
                    CheckScan(_scanDir, card.localPosition.z, Time.time);

                _scanDir = newdir;

                _firstTime = Time.time;
            }
        }
        else
            _scanDir = 0;
    }

    private void CheckScan(int mode, float lastPos, float lastTime)
    {
        float time = lastTime - _firstTime;
        if(time > 0.2f)
            return;

        Log("Scanned a card (number " + Card.Held.Number + ") in a " + (mode == 1 ? "downward" : "upward") + " direction.");
        GetComponent<KMAudio>().PlaySoundAtTransform("Beep", transform);

        if(_symbolIx < 8 && mode == 2 || _symbolIx > 7 && mode == 1)
        {
            Strike(true);
            return;
        }

        IEnumerable<int> sn = GetComponent<KMBombInfo>().QueryWidgets(KMBombInfo.QUERYKEY_GET_SERIAL_NUMBER, null).First()
            .Where(c => "0123456789".Contains(c)).Select(c => int.Parse(c.ToString()));
        int order = sn.First() > sn.Last() ? 1 : -1;

        Card correct = CardFolder.Instance.GetAvailableCards()
            .OrderByDescending(c => {
                IEnumerable<char> chr = c.Number.Where(ch => "0123456789".Contains(ch));
                int i = int.Parse(chr.Skip(_symbolIx % 8).Concat(chr.Take(_symbolIx % 8)).Join(""));
                return order * i; })
            .First();

        if(correct == Card.Held)
            Solve();
        else
            Strike(false);
    }

    private void Solve()
    {
        if(!_unSolved)
            return;

        _led.material = _lightMats[1];
        GetComponentInChildren<KeyCardAcceptor>().OnCollide -= CardCollide;
        GetComponentInChildren<KeyCardAcceptor>().Active = false; ;
        _unSolved = false;

        Log("Correct card scanned; Module solved.");
        GetComponent<KMBombModule>().HandlePass();
    }

    private void Log(string v)
    {
        Debug.Log("[Swipe The Key #" + _id + "] " + v);
    }

    private void Strike(bool dir)
    {
        if(!_unSolved)
            return;

        StartCoroutine(Flash());

        Log("Incorrect " + (dir ? "scan direction" : "card scanned") + "; Strike issued.");
        GetComponent<KMBombModule>().HandleStrike();
    }

    private IEnumerator Flash()
    {
        _led.material = _lightMats[2];
        yield return new WaitForSeconds(1f);
        _led.material = _lightMats[0];
    }

    private void EnsureFolder()
    {
        if(CardFolder.Instance != null)
            return;

        MonoBehaviour room = (MonoBehaviour)FindObjectOfType(ReflectionHelper.FindTypeInGame("GameplayRoom"));
        Transform[] modholdables = FindObjectsOfType(ReflectionHelper.FindTypeInGame("ModHoldable")).Cast<MonoBehaviour>().Select(m => m.transform).ToArray();
        IList spawns = room.GetType().Field<IList>("HoldableSpawnPoints", room);
        MonoBehaviour hsp = spawns.Cast<MonoBehaviour>().First(hspt => !modholdables.Any(tr => (tr.position - hspt.transform.position).magnitude < 0.01f));
        GameObject mho = Instantiate(_folder.gameObject, hsp.transform.position, hsp.transform.rotation);
        Type mht = ReflectionHelper.FindTypeInGame("ModHoldable");
        Component mh;

        Type mselt = ReflectionHelper.FindTypeInGame("ModSelectable");

        if(!(mh = mho.GetComponent(mht)))
            mh = mho.AddComponent(mht);
        if(!mho.GetComponent(mselt))
            mho.AddComponent(mselt);

        Type selt = ReflectionHelper.FindTypeInGame("Selectable");
        selt.SetField("Parent", mh.GetComponent(selt), room.GetComponent(selt));
        mh.transform.parent = room.transform;
        mh.transform.localScale = Vector3.one;
        mh.GetType().SetField("HoldableTarget", mh, hsp.GetType().Field<object>("HoldableTarget", hsp));
        int ix = selt.Field<int>("ChildRowLength", room.GetComponent(selt)) * hsp.GetType().Field<int>("SelectableIndexY", hsp) + hsp.GetType().Field<int>("SelectableIndexX", hsp);
        selt.Field<IList>("Children", room.GetComponent(selt))[ix] = mh.GetComponent(selt);

        object arr = Array.CreateInstance(ReflectionHelper.FindTypeInGame("Assets.Scripts.Input.FaceSelectable"), 0);
        mht.SetField("Faces", mh, arr);
    }
}
