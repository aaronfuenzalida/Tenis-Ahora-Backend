"""
TC_001 - Validar el bloqueo de reserva por falta de stock disponible de equipamiento deportivo.

Módulo: Gestión de Stock. Prioridad: Medium.
Precondición: pelotas en 0 (redes 2, raquetas 4) y usuario autenticado.

En el documento el paso 4 falló: la reserva se confirmó igual con el stock en 0. Acá se verifica
que la reserva no se registra y que tampoco se puede cobrar la seña de una reserva anotada antes de
que se agotaran las pelotas.

Uso:  python tests/tc_001_bloqueo_sin_stock.py [http://localhost:5090/api]
"""
import sys

from casos_tp import ejecutar
from smoke import call, check, in_days, pay, stock_of

ID = "TC_001"
TITULO = "Bloqueo de reserva por falta de stock de equipamiento"

CANCHA = "cancha-1"  # C-01, polvo de ladrillo
JUGADORES = [{"name": "Valentín Acosta", "dni": "40.111.001", "phone": "11 4000-0001", "isLead": True},
             {"name": "Bruno Salas", "dni": "40.111.002", "phone": "11 4000-0002"}]
OTROS = [{"name": "Camila Ruiz", "dni": "40.111.003", "phone": "11 4000-0003", "isLead": True},
         {"name": "Paula Vega", "dni": "40.111.004", "phone": "11 4000-0004"}]


def ajustar_stock(category, total):
    call("PATCH", f"/stock/{stock_of(category)['id']}/ajuste", {"totalStock": total}, expect=200)


def run():
    call("POST", "/admin/reset", expect=200)
    # Reserva anotada mientras todavía había pelotas: sirve para el intento de cobro del paso 4.
    st, previa = call("POST", "/reservas", {"courtId": CANCHA, "date": in_days(2), "startTime": "10:00", "durationHours": 1,
                                            "playersType": "singles", "participants": OTROS}, expect=201)
    previa = previa["data"]
    ajustar_stock("Pelotas", 0)
    ajustar_stock("Redes", 2)
    ajustar_stock("Raquetas", 4)
    niveles = {c: stock_of(c)["availableStock"] for c in ("Pelotas", "Redes", "Raquetas")}
    check("TC_001 precondición - stock: pelotas 0, redes 2, raquetas 4",
          niveles == {"Pelotas": 0, "Redes": 2, "Raquetas": 4}, str(niveles))

    st, reservas = call("GET", "/reservas")
    check("TC_001 paso 1 - el usuario autenticado accede al módulo de reservas", st == 200 and isinstance(reservas, list))

    st, cancha = call("GET", f"/canchas/{CANCHA}", expect=200)
    disp = cancha["availability"]
    check("TC_001 paso 2 - cancha C-01 de ladrillo disponible en el horario elegido",
          cancha["surfaceType"] == "Ladrillo" and cancha["status"] == "disponible"
          and disp["openTime"] <= "18:00" and "19:00" <= disp["closeTime"], cancha["name"])

    reserva = {"courtId": CANCHA, "date": in_days(1), "startTime": "18:00", "durationHours": 1,
               "playersType": "singles", "participants": JUGADORES}
    st, err = call("POST", "/reservas", reserva)
    msg = (err or {}).get("error", "") if isinstance(err, dict) else str(err)
    check("TC_001 paso 3 - el sistema detecta que faltan pelotas y no registra la reserva (409)",
          st == 409 and "pelotas" in msg and "redes" not in msg and "raquetas" not in msg, msg)

    st, tx = pay(previa["depositAmount"])
    st, _ = call("POST", f"/reservas/{previa['id']}/pagar-sena", {"transactionId": tx["id"]})
    st_prev, prev_now = call("GET", f"/reservas/{previa['id']}", expect=200)
    check("TC_001 paso 4 - el cobro de la seña se interrumpe sin stock (409) y la reserva no se confirma",
          st == 409 and prev_now["status"] == "pendiente_pago" and prev_now["depositPaid"] == 0, prev_now["status"])

    st, reservas = call("GET", "/reservas", expect=200)
    creada = [r for r in reservas if r["courtId"] == CANCHA and r["date"] == reserva["date"] and r["startTime"] == "18:00"]
    check("TC_001 poscondición - no hay reserva registrada para ese turno y las pelotas siguen en 0",
          not creada and stock_of("Pelotas")["availableStock"] == 0)


if __name__ == "__main__":
    sys.exit(ejecutar([sys.modules[__name__]]))
