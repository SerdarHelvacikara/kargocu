using System;
using UnityEngine;

namespace KurmaliKargo
{
    public sealed partial class CourierGame
    {
        NeighborhoodIncidents incidents;
        Action<Incident> announceIncident;
        float cyclistNoticeAt;
        internal int CyclistYieldCount { get; private set; }
        internal NeighborhoodIncidents IncidentScene => incidents;

        void TickIncidents(float dt)
        {
            if (Phase != GamePhase.Playing || deliveryCelebration > 0) return;
            if (!incidents.Tick(dt, player.position, velocity.magnitude, ReducedMotion, announceIncident)) return;
            Coins += 8;
            Notify(GameText.Pick("Ördek ailesine yol verdin! +8 jeton", "Thanks for letting the ducks pass! +8 coins"));
            PlayNote(880);
            Save();
        }

        void AnnounceIncident(Incident kind)
        {
            switch (kind)
            {
                case Incident.Ducks: Notify(GameText.Pick("Ördek ailesi geçiyor. Beklersen +8 jeton!", "Duck family crossing. Wait for +8 coins!")); break;
                case Incident.BurstPipe: Notify(GameText.Pick("Su borusu patladı. Yavaş dön, zemin kaygan!", "Burst pipe. Turn gently on the slick road!")); break;
                case Incident.Market: Notify(GameText.Pick("Pazar kuruldu. Ortadaki geçit açık!", "Market day. The middle lane is open!")); break;
                case Incident.Wedding:
                    Notify(GameText.Pick("Düğün konvoyu geçiyor. Diğer sokak açık!", "Wedding convoy crossing. Take the next street!"));
                    PlayFeelPair(330, 440);
                    if (!ReducedMotion) SpawnFeelParticles(NeighborhoodIncidents.Position(kind) + Vector3.up, false, 12);
                    break;
                case Incident.Wind: Notify(GameText.Pick("Rüzgâr çıktı! Hafif kutular sallanıyor.", "Gusty wind! Light parcels are wobbling.")); break;
            }
        }

        Vector3 ConstrainTrafficStep(Vector3 step)
        {
            float margin = TrafficMargin;
            if (incidents.LaneBlocked) step = incidents.ConstrainStep(player.position, step, margin);
            if (!world.City) return step;
            Vector3 allowed = world.City.ClampAtCyclist(player.position, step, margin);
            bool blocked = (step - allowed).sqrMagnitude > .000001f;
            if (blocked) CyclistYieldCount++;
            if (blocked && Time.unscaledTime >= cyclistNoticeAt)
            {
                cyclistNoticeAt = Time.unscaledTime + 2.4f;
                Notify(GameText.Pick("Bisikletliye yol ver!", "Give the cyclist room!"));
                PlayNote(590);
            }
            return allowed;
        }
    }
}
