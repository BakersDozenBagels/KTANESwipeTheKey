using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(KMSelectable), typeof(KMHoldable), typeof(Animator))]
public class CardFolder : MonoBehaviour
{
    [RummageNoRename]
    [SerializeField]
    private Transform[] _slots;
    [RummageNoRename]
    [SerializeField]
    private Card _cardPerfab;

    public static CardFolder Instance
    {
        get;
        private set;
    }

    private readonly List<Card> _cards = new List<Card>();

    public List<Card> GetAvailableCards()
    {
        return _cards;
    }

    [RummageNoRename]
    [RummageNoRemove]
    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
        {
            Debug.LogError("[Swipe The Key] A CardFolder was created, but one already exists! No new folder will be created.");
            Destroy(gameObject);
        }
    }

    [RummageNoRename]
    [RummageNoRemove]
    private void Start()
    {
        KMSelectable self = GetComponent<KMSelectable>();
        List<KMSelectable> children = new List<KMSelectable>();
        foreach(Transform slot in _slots)
        {
            Card c = Instantiate(_cardPerfab.gameObject, slot, false).GetComponent<Card>();
            c.Init();
            children.Add(c.GetComponent<KMSelectable>());
            children.Last().Parent = self;
            _cards.Add(c);
        }
        self.Children = children.ToArray();
        self.UpdateChildrenProperly();

        Debug.Log("[Swipe The Key] Available card numbers are: " + _cards.Select(c => c.Number).Join(" "));

        Type fht = ReflectionHelper.FindTypeInGame("FloatingHoldable");
        Component fh = GetComponent(fht);
        Action hold = fht.Field<Action>("OnHold", fh);
        Action letGo = fht.Field<Action>("OnLetGo", fh);

        fht.SetField<Action>("OnHold", fh, () => { GetComponent<Animator>().SetBool("Open", true); if(hold != null) hold(); });
        fht.SetField<Action>("OnLetGo", fh, () => { GetComponent<Animator>().SetBool("Open", false); if(letGo != null) letGo(); });
    }

    [RummageNoRename]
    [RummageNoRemove]
    private void OnDestroy()
    {
        if(Instance == this)
        {
            Debug.Log("[Swipe The Key] CardFolder destroyed. Card numbers are reset.");
            Instance = null;
        }
    }
}
