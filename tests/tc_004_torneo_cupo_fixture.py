"""
TC_004 - Validar el registro de torneos, el control de cupos máximos de inscripción por categoría y
la generación del fixture.

Módulo: Gestión de Torneos. Prioridad: High.
Precondición: torneo "Copa Primavera 2026" (TR-2026) abierto, categoría Dobles Caballeros con cupo
de 16 parejas.

La llave y el avance del ganador los calcula el panel de administración
(Tenis-Ahora-Frontend/src/modulos/competencias/TournamentBracketTree.jsx) y los guarda con
PUT /torneos/{id}. Los pasos 3 y 4 repiten ese cálculo y verifican lo que la API deja guardado.

Uso:  python tests/tc_004_torneo_cupo_fixture.py [http://localhost:5090/api]
"""
import copy
import sys

from casos_tp import ejecutar
from smoke import call, check

ID = "TC_004"
TITULO = "Registro de torneo, cupo máximo por categoría y fixture"

TORNEO = "TR-2026"
CUPO = 16
RONDAS = ["Octavos de final", "Cuartos de final", "Semifinales", "Final"]


def pareja(n):
    return {"name": f"Jugador {n}A", "dni": f"41.{n:03d}.001", "phone": f"11 4100-{n:04d}",
            "partnerName": f"Jugador {n}B", "partnerDni": f"41.{n:03d}.002"}


def nombre(p):
    return f"{p['name']} / {p['partnerName']}" if p.get("partnerName") else p["name"]


def armar_llave(participantes):
    # Igual que buildBracketFromParticipants: octavos con los inscriptos en orden y las rondas
    # siguientes con "Ganador partido N".
    rondas, numero, partidos = [], 1, CUPO // 2
    for r in range(4):
        primero_anterior = numero - partidos * 2
        lista = []
        for i in range(partidos):
            if r == 0:
                p1, p2, estado = nombre(participantes[i * 2]), nombre(participantes[i * 2 + 1]), "programado"
            else:
                p1, p2, estado = f"Ganador partido {primero_anterior + i * 2}", f"Ganador partido {primero_anterior + i * 2 + 1}", "pendiente"
            lista.append({"id": f"m{numero}", "p1": p1, "p2": p2, "score": "", "winner": None, "status": estado})
            numero += 1
        rondas.append({"round": RONDAS[r], "matches": lista})
        partidos //= 2
    return rondas


def cargar_resultado(llave, match_id, score, winner):
    # Igual que handleSaveResult: cierra el partido y pasa al ganador a la ronda siguiente.
    llave = copy.deepcopy(llave)
    for ri, ronda in enumerate(llave):
        for mi, m in enumerate(ronda["matches"]):
            if m["id"] == match_id:
                m.update(score=score, winner=winner, status="finalizado")
                if ri < len(llave) - 1:
                    destino = llave[ri + 1]["matches"][mi // 2]
                    destino["p1" if mi % 2 == 0 else "p2"] = winner
                    if destino["status"] != "finalizado":
                        destino["status"] = "programado"
    return llave


def run():
    call("POST", "/admin/reset", expect=200)
    st, t = call("POST", "/torneos", {"id": TORNEO, "name": "Copa Primavera 2026", "category": "Masculino",
                                      "modality": "Dobles", "surfaceRequired": "Ladrillo", "maxParticipants": CUPO,
                                      "registrationFee": 12000, "status": "inscripcion_abierta"}, expect=201)
    check("TC_004 precondición - torneo abierto, Dobles Caballeros, cupo de 16 parejas",
          t["id"] == TORNEO and t["status"] == "inscripcion_abierta" and t["maxParticipants"] == CUPO)

    estados = [call("POST", f"/torneos/{TORNEO}/inscripciones", pareja(n))[0] for n in range(1, CUPO + 1)]
    st, t = call("GET", f"/torneos/{TORNEO}", expect=200)
    check("TC_004 paso 1 - registra las 16 parejas con el DNI de ambos integrantes y muestra 16/16",
          all(s == 201 for s in estados) and t["currentEnrolled"] == CUPO and len(t["participants"]) == CUPO
          and all(p["dni"] and p["partnerDni"] for p in t["participants"]), f"{t['currentEnrolled']}/{t['maxParticipants']}")

    st, err = call("POST", f"/torneos/{TORNEO}/inscripciones", {"name": "Pareja 17A", "dni": "38112233",
                                                                 "partnerName": "Pareja 17B", "partnerDni": "39445566"})
    msg = (err or {}).get("error", "") if isinstance(err, dict) else str(err)
    st_t, t = call("GET", f"/torneos/{TORNEO}", expect=200)
    check("TC_004 paso 2 - bloquea la pareja 17 con cupo completo y muestra el aviso",
          st == 409 and "cupo" in msg and t["currentEnrolled"] == CUPO, msg)

    llave = armar_llave(t["participants"])
    call("PUT", f"/torneos/{TORNEO}", {"status": "en_curso", "bracket": llave}, expect=200)
    st, t = call("GET", f"/torneos/{TORNEO}", expect=200)
    octavos = t["bracket"][0]
    en_cuadro = [n for m in octavos["matches"] for n in (m["p1"], m["p2"])]
    check("TC_004 paso 3 - cierra la inscripción y genera la llave con 8 partidos de octavos de final",
          t["status"] == "en_curso" and octavos["round"] == "Octavos de final" and len(octavos["matches"]) == 8
          and sorted(en_cuadro) == sorted(nombre(p) for p in t["participants"])
          and [len(r["matches"]) for r in t["bracket"]] == [8, 4, 2, 1])

    partido = octavos["matches"][0]
    pareja_a = partido["p1"]
    llave = cargar_resultado(t["bracket"], partido["id"], "6-4, 6-3", pareja_a)
    call("PUT", f"/torneos/{TORNEO}", {"bracket": llave}, expect=200)
    st, t = call("GET", f"/torneos/{TORNEO}", expect=200)
    m1 = t["bracket"][0]["matches"][0]
    cuartos = t["bracket"][1]
    check("TC_004 paso 4 - registra el resultado del partido 1 y la pareja A pasa a cuartos de final",
          m1["status"] == "finalizado" and m1["winner"] == pareja_a and m1["score"] == "6-4, 6-3"
          and cuartos["round"] == "Cuartos de final" and cuartos["matches"][0]["p1"] == pareja_a, pareja_a)

    check("TC_004 poscondición - el cupo sigue en 16 y el cuadro refleja el avance del ganador",
          len(t["participants"]) == CUPO and t["bracket"][1]["matches"][0]["status"] == "programado")


if __name__ == "__main__":
    sys.exit(ejecutar([sys.modules[__name__]]))
