"""
TC_003 - Validar los datos de entrada de una reserva según las reglas de negocio del club.

Módulo: Gestión de Reservas. Prioridad: High.
Precondición: cancha C-03 (cemento) habilitada a $10.000 la hora y stock suficiente.

Fechas del documento -> fechas usadas:
  13/10/2026 (turno válido)    -> mañana
  20/11/2026 (39 días después) -> hoy + 39 días
  10/10/2026 (fecha pasada)    -> hace 3 días

Uso:  python tests/tc_003_validacion_reserva.py [http://localhost:5090/api]
"""
import sys

from casos_tp import ejecutar
from smoke import VISA_CREDITO, call, check, in_days, pay

ID = "TC_003"
TITULO = "Validación de los datos de entrada de una reserva"

CANCHA = "cancha-3"  # C-03, cemento
JUGADORES = [{"name": "Martina Sosa", "dni": "40.333.001", "phone": "11 4000-2001", "isLead": True},
             {"name": "Julieta Paz", "dni": "40.333.002", "phone": "11 4000-2002"}]
VALIDA = {"courtId": CANCHA, "date": in_days(1), "startTime": "18:00", "durationHours": 2,
          "playersType": "singles", "participants": JUGADORES}


def rechazo(cambios):
    st, err = call("POST", "/reservas", dict(VALIDA, **cambios))
    return st, (err or {}).get("error", "") if isinstance(err, dict) else str(err)


def run():
    call("POST", "/admin/reset", expect=200)
    st, cancha = call("PUT", f"/canchas/{CANCHA}", {"pricePerHour": 10000}, expect=200)
    check("TC_003 precondición - cancha C-03 de cemento habilitada a $10.000 la hora",
          cancha["surfaceType"] == "Cemento" and cancha["status"] == "disponible" and cancha["pricePerHour"] == 10000)

    st, msg = rechazo({"durationHours": 3})  # 18:00 a 21:00
    check("TC_003 paso 1 - rechaza una reserva de 3 horas e informa que el máximo es 2 horas",
          st == 400 and "2 horas" in msg, msg)

    st, msg = rechazo({"date": in_days(39)})
    check("TC_003 paso 2 - rechaza una reserva con 39 días de antelación (máximo 30)",
          st == 400 and "30" in msg, msg)

    st, msg = rechazo({"date": in_days(-3)})
    check("TC_003 paso 3 - rechaza una reserva con fecha anterior a la actual",
          st == 400 and "pasó" in msg, msg)

    tres = JUGADORES + [{"name": "Rocío Gil", "dni": "40.333.003", "phone": "11 4000-2003"}]
    st_singles, _ = rechazo({"participants": tres})
    st_dobles, _ = rechazo({"playersType": "dobles", "participants": tres})
    st, res = call("POST", "/reservas", VALIDA)
    check("TC_003 paso 4 - acepta 2 participantes y rechaza 3",
          st == 201 and st_singles == 400 and st_dobles == 400, f"3 jugadores -> {st_singles}/{st_dobles}, 2 jugadores -> {st}")
    if st != 201:
        raise AssertionError(f"POST /reservas -> {st}: {res}")
    rid = res["data"]["id"]

    st, guardada = call("GET", f"/reservas/{rid}", expect=200)
    check("TC_003 paso 5 - queda \"Pendiente de pago\" con total $20.000 y seña $10.000 (50 %)",
          guardada["status"] == "pendiente_pago" and guardada["totalCost"] == 20000 and guardada["depositAmount"] == 10000
          and guardada["endTime"] == "20:00", rid)

    st, tx = pay(10000, "Tarjeta de Crédito", VISA_CREDITO)
    st, conf = call("POST", f"/reservas/{rid}/pagar-sena", {"transactionId": tx["id"]}, expect=200)
    check("TC_003 paso 6 - cobra la seña con crédito, emite el recibo y la reserva pasa a \"Confirmada\"",
          conf["data"]["status"] == "confirmada" and conf["receipt"]["totalPaid"] == 10000
          and conf["receipt"]["paymentMethod"] == "Tarjeta de Crédito", conf["receipt"]["id"])

    st, final = call("GET", f"/reservas/{rid}", expect=200)
    datos = [{k: p.get(k) for k in ("name", "dni", "phone")} for p in final["participants"]]
    check("TC_003 poscondición - seña igual al 50 % del total y datos de los participantes guardados",
          final["depositPaid"] == final["totalCost"] / 2
          and datos == [{k: p[k] for k in ("name", "dni", "phone")} for p in JUGADORES])


if __name__ == "__main__":
    sys.exit(ejecutar([sys.modules[__name__]]))
