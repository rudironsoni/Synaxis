import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts'

const data = [
  { name: 'Mon', requests: 4000 },
  { name: 'Tue', requests: 3000 },
  { name: 'Wed', requests: 5000 },
  { name: 'Thu', requests: 4500 },
  { name: 'Fri', requests: 6000 },
  { name: 'Sat', requests: 3500 },
  { name: 'Sun', requests: 4200 },
]

export function RequestsChart() {
  return (
    <ResponsiveContainer width="100%" height={300}>
      <LineChart data={data}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="name" />
        <YAxis />
        <Tooltip />
        <Line type="monotone" dataKey="requests" stroke="#8884d8" strokeWidth={2} />
      </LineChart>
    </ResponsiveContainer>
  )
}
